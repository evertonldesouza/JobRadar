using System.Text.Json;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;

namespace JobRadar.Infrastructure.Sources;

public class CareerNestSource : IJobSource
{
    private readonly HttpClient _httpClient;
    public string Name => "CareerNest";

    public CareerNestSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Job>> FetchJobsAsync()
    {
        var response = await _httpClient.GetAsync("https://careernest.cloud/api/feed?limit=50");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var root = JsonSerializer.Deserialize<JsonElement>(json);
        var rawJobs = root.GetProperty("jobs").EnumerateArray();

        var jobs = new List<Job>();

        foreach (var item in rawJobs)
        {
            try
            {
                var title = item.GetProperty("title").GetString() ?? string.Empty;
                var company = item.GetProperty("company").GetString() ?? string.Empty;
                var location = item.GetProperty("location").GetString() ?? string.Empty;
                var url = item.GetProperty("job_url").GetString() ?? string.Empty;

                var tags = new List<string>();
                if (item.TryGetProperty("category", out var cat))
                    tags.Add(cat.GetString() ?? "");
                if (item.TryGetProperty("job_type", out var jt))
                    tags.Add(jt.GetString() ?? "");

                var dateStr = item.GetProperty("posted_at").GetString() ?? string.Empty;
                var publishedAt = DateTime.TryParse(dateStr, out var dt)
                    ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                    : DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
                    continue;

                jobs.Add(Job.Create(title, company, location, url, publishedAt, tags, Name));
            }
            catch
            {
                continue;
            }
        }

        return jobs;
    }
}
