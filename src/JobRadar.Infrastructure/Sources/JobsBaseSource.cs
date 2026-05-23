using System.Text.Json;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;

namespace JobRadar.Infrastructure.Sources;

public class JobsBaseSource : IJobSource
{
    private readonly HttpClient _httpClient;
    public string Name => "JobsBase";

    public JobsBaseSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Job>> FetchJobsAsync()
    {
        var response = await _httpClient.GetAsync("https://jobsbase.io/api/v1/jobs?limit=50");
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
                var location = item.TryGetProperty("display_location", out var loc)
                    ? loc.GetString() ?? "Remote"
                    : "Remote";
                var url = item.GetProperty("job_url").GetString() ?? string.Empty;

                var tags = item.TryGetProperty("skills", out var skillsEl)
                    ? skillsEl.EnumerateArray().Select(s => s.GetString() ?? "").ToList()
                    : [];

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
