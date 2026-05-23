using System.Text.Json;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;

namespace JobRadar.Infrastructure.Sources;

public class RemotiveSource : IJobSource
{
    private readonly HttpClient _httpClient;
    public string Name => "Remotive";

    public RemotiveSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Job>> FetchJobsAsync()
    {
        var response = await _httpClient.GetAsync("https://remotive.com/api/remote-jobs");
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
                var company = item.GetProperty("company_name").GetString() ?? string.Empty;
                var url = item.GetProperty("url").GetString() ?? string.Empty;
                var location = item.TryGetProperty("candidate_required_location", out var loc)
                    ? loc.GetString() ?? "Remote"
                    : "Remote";

                var tags = item.TryGetProperty("tags", out var tagsEl)
                    ? tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").ToList()
                    : [];

                var dateStr = item.GetProperty("publication_date").GetString() ?? string.Empty;
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
