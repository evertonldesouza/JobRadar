using JobRadar.Domain.Entities;

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
)
{
    /// <summary>Cria o DTO a partir da entidade (nunca expondo a entidade na borda).</summary>
    public static JobDto FromJob(Job job) => new(
        job.Id,
        job.Title,
        job.Company,
        job.Location,
        job.Url,
        job.PublishedAt,
        job.Technologies,
        job.Source);
}
