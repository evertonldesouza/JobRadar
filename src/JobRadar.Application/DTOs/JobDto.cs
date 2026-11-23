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
    string Source,
    decimal? SalaryMin = null,
    decimal? SalaryMax = null,
    string? SalaryCurrency = null,
    string? SalaryPeriod = null
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
        job.Source,
        job.SalaryMin,
        job.SalaryMax,
        job.SalaryCurrency,
        job.SalaryPeriod);

    /// <summary>Faixa pronta para exibição (ex.: "USD 60.000 – 90.000/ano"), quando informada.</summary>
    public string? SalaryText
    {
        get
        {
            if (SalaryMin is null || string.IsNullOrWhiteSpace(SalaryCurrency))
                return null;

            var currency = SalaryCurrency.ToUpperInvariant();
            var periodLabel = (SalaryPeriod ?? "year") switch
            {
                "month" => "/mês",
                "week" => "/semana",
                "day" => "/dia",
                "hour" => "/hora",
                _ => "/ano"
            };

            var min = SalaryMin.Value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
            if (SalaryMax is null || SalaryMax < SalaryMin)
                return $"{currency} {min}{periodLabel}";

            var max = SalaryMax.Value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
            return $"{currency} {min} – {max}{periodLabel}";
        }
    }
}
