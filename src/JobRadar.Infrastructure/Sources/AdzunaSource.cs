using System.Text.Json;
using JobRadar.Domain.Entities;
using JobRadar.Domain.ValueObjects;
using JobRadar.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JobRadar.Infrastructure.Sources;

public class AdzunaSource : IJobSource
{
    private readonly HttpClient _httpClient;
    private readonly string _appId;
    private readonly string _appKey;
    public string Name => "Adzuna";

    public AdzunaSource(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _appId = configuration["Adzuna:AppId"]!;
        _appKey = configuration["Adzuna:AppKey"]!;
    }

    public async Task<IEnumerable<Job>> FetchJobsAsync()
    {
        var url = $"https://api.adzuna.com/v1/api/jobs/gb/search/1" +
                  $"?app_id={_appId}&app_key={_appKey}" +
                  $"&results_per_page=50&what=developer&content-type=application/json";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var root = JsonSerializer.Deserialize<JsonElement>(json);

        var jobs = new List<Job>();

        foreach (var item in root.GetProperty("results").EnumerateArray())
        {
            try
            {
                var title = item.GetProperty("title").GetString() ?? string.Empty;
                var company = item.TryGetProperty("company", out var comp)
                    ? comp.GetProperty("display_name").GetString() ?? string.Empty
                    : string.Empty;
                var location = item.TryGetProperty("location", out var loc)
                    ? loc.GetProperty("display_name").GetString() ?? string.Empty
                    : string.Empty;
                var url2 = item.GetProperty("redirect_url").GetString() ?? string.Empty;
                var createdStr = item.GetProperty("created").GetString() ?? string.Empty;
                var publishedAt = DateTime.TryParse(createdStr, out var dt) 
                    ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) 
                    : DateTime.UtcNow;
                var description = item.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? string.Empty
                    : string.Empty;

                var technologies = ExtractTechnologies(title + " " + description);

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url2))
                    continue;

                jobs.Add(Job.Create(title, company, location, url2, publishedAt, technologies, Name,
                    ExtractSalary(item)));
            }
            catch
            {
                continue;
            }
        }

        return jobs;
    }

    /// <summary>
    /// A Adzuna traz salary_min/salary_max (GBP, base anual) e salary_is_predicted:
    /// valores estimados pela própria Adzuna são descartados para não misturar
    /// dado real com palpite.
    /// </summary>
    private static SalaryRange? ExtractSalary(JsonElement item)
    {
        if (item.TryGetProperty("salary_is_predicted", out var predicted) &&
            predicted.ValueKind == JsonValueKind.String &&
            predicted.GetString() == "1")
            return null;

        var min = ReadDecimal(item, "salary_min");
        var max = ReadDecimal(item, "salary_max");

        if (min is null && max is null)
            return null;

        var low = min ?? max!.Value;
        var high = max ?? min!.Value;

        return new SalaryRange(low, high, "GBP", SalaryPeriod.Year);
    }

    private static decimal? ReadDecimal(JsonElement item, string property) =>
        item.TryGetProperty(property, out var el) &&
        el.ValueKind is JsonValueKind.Number or JsonValueKind.String &&
        decimal.TryParse(el.ValueKind == JsonValueKind.Number ? el.GetRawText() : el.GetString(),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value) &&
        value > 0
            ? value
            : null;

    private static List<string> ExtractTechnologies(string text)
    {
        var keywords = new[] { "c#", ".net", "python", "javascript", "typescript",
            "react", "angular", "vue", "node", "java", "sql", "docker",
            "kubernetes", "aws", "azure", "gcp", "php", "ruby", "go", "rust" };

        return keywords
            .Where(k => text.Contains(k, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}