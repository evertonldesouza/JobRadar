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
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título da vaga é obrigatório.", nameof(title));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL da vaga é obrigatória.", nameof(url));
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("URL da vaga deve ser uma URL http(s) válida.", nameof(url));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Fonte da vaga é obrigatória.", nameof(source));

        return new Job
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Company = company?.Trim() ?? string.Empty,
            Location = string.IsNullOrWhiteSpace(location) ? "Remote" : location.Trim(),
            Url = url.Trim(),
            PublishedAt = DateTime.SpecifyKind(publishedAt, DateTimeKind.Utc),
            Technologies = (technologies ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList(),
            Source = source.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Chave de deduplicação (título + empresa), normalizada.</summary>
    public string DeduplicationKey => $"{Title.Trim().ToLowerInvariant()}|{Company.Trim().ToLowerInvariant()}";
}
