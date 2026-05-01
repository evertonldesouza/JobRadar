namespace JobRadar.Application.DTOs;

public record JobDto(
    Guid Id,
    string Title,
    string Company,
    string Location,
    string Url,
    DateTime PublishedAt,
    List<string> Technologies,
    string Source
);
