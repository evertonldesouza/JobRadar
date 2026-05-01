namespace JobRadar.Domain.Entities;

public class Job
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Company { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public DateTime PublishedAt { get; private set; }
    public List<string> Technologies { get; private set; } = [];
    public string Source { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    protected Job() { }

    public static Job Create(
        string title,
        string company,
        string location,
        string url,
        DateTime publishedAt,
        List<string> technologies,
        string source)
    {
        return new Job
        {
            Id = Guid.NewGuid(),
            Title = title,
            Company = company,
            Location = location,
            Url = url,
            PublishedAt = publishedAt,
            Technologies = technologies,
            Source = source,
            CreatedAt = DateTime.UtcNow
        };
    }
}