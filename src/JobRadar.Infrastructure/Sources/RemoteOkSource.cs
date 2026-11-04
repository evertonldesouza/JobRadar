using System.Text.Json;
using JobRadar.Domain.Entities;
using JobRadar.Domain.ValueObjects;
using JobRadar.Domain.Interfaces;

namespace JobRadar.Infrastructure.Sources;

public class RemoteOkSource : IJobSource
{
    private readonly HttpClient _httpClient;
    public string Name => "RemoteOK";

    public RemoteOkSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "JobRadar/1.0");
    }

    public async Task<IEnumerable<Job>> FetchJobsAsync()
    {
        var response = await _httpClient.GetAsync("https://remoteok.com/remote-jobs.json");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var rawJobs = JsonSerializer.Deserialize<List<JsonElement>>(json);

        if (rawJobs is null) return [];

        var jobs = new List<Job>();

        foreach (var item in rawJobs.Skip(1)) // primeiro item é metadata
        {
            try
            {
                var title = item.GetProperty("position").GetString() ?? string.Empty;
                var company = item.GetProperty("company").GetString() ?? string.Empty;
                var url = item.GetProperty("url").GetString() ?? string.Empty;
                var location = item.TryGetProperty("location", out var loc)
                    ? loc.GetString() ?? "Remote"
                    : "Remote";

                var tags = item.TryGetProperty("tags", out var tagsEl)
                    ? tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").ToList()
                    : new List<string>();

                var epochStr = item.GetProperty("epoch").GetInt64();
                var publishedAt = DateTimeOffset.FromUnixTimeSeconds(epochStr).UtcDateTime;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
                    continue;

                jobs.Add(Job.Create(title, company, location, url, publishedAt, tags, Name,
                    ExtractSalary(item)));
            }
            catch
            {
                continue;
            }
        }

        return jobs;
    }

    /// <summary>RemoteOK expõe salary_min/salary_max em USD por ano (0 = não informado).</summary>
    private static SalaryRange? ExtractSalary(JsonElement item)
    {
        var min = ReadAmount(item, "salary_min");
        var max = ReadAmount(item, "salary_max");

        if (min is null && max is null)
            return null;

        return new SalaryRange(min ?? max!.Value, max ?? min!.Value, "USD", SalaryPeriod.Year);
    }

    private static decimal? ReadAmount(JsonElement item, string property) =>
        item.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.Number &&
        el.TryGetDecimal(out var value) && value > 0
            ? value
            : null;
}