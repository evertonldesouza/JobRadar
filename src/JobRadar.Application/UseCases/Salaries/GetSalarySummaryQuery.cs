using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using JobRadar.Domain.ValueObjects;
using MediatR;

namespace JobRadar.Application.UseCases.Salaries;

/// <summary>
/// Comparativo salarial por tecnologia. Só entram vagas cujo anúncio informa
/// faixa salarial; a cobertura é exposta junto para não induzir a erro.
/// </summary>
public record GetSalarySummaryQuery(
    string? Currency = null,
    int MinimumSamples = 3,
    int MaxTechnologies = 12
) : IRequest<SalarySummaryDto>;

public sealed class GetSalarySummaryQueryHandler : IRequestHandler<GetSalarySummaryQuery, SalarySummaryDto>
{
    // Faixa de sanidade para o equivalente anual (evita valores claramente errados).
    private const decimal MinAnnual = 1_000m;
    private const decimal MaxAnnual = 1_000_000m;

    public const string DisclaimerText =
        "Somente vagas cujo anúncio informa salário entram nas estatísticas; " +
        "valores são convertidos para a base anual a partir do período informado.";

    private readonly IJobRepository _jobRepository;

    public GetSalarySummaryQueryHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<SalarySummaryDto> Handle(GetSalarySummaryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _jobRepository.GetSalaryEntriesAsync(cancellationToken);
        var totalJobs = await _jobRepository.CountAsync(cancellationToken);

        var byCurrency = entries
            .GroupBy(e => e.Currency.ToUpperInvariant())
            .Select(g => new SalaryCurrencyOptionDto(g.Key, g.Count()))
            .OrderByDescending(c => c.Samples)
            .ThenBy(c => c.Currency)
            .ToList();

        var currency = ResolveCurrency(request.Currency, byCurrency);
        var samples = entries
            .Where(e => string.Equals(e.Currency, currency, StringComparison.OrdinalIgnoreCase))
            .Select(ToAnnualMidpoint)
            .Where(value => value is >= MinAnnual and <= MaxAnnual)
            .ToList();

        var coverage = new SalaryCoverageDto(
            entries.Count,
            totalJobs,
            totalJobs == 0 ? 0m : Math.Round(entries.Count * 100m / totalJobs, 1));

        var technologies = samples
            .SelectMany(sample => sample.Technologies
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .Select(tech => (Technology: tech, sample.Annual)))
            .GroupBy(x => x.Technology)
            .Where(g => g.Count() >= request.MinimumSamples)
            .Select(g =>
            {
                var values = g.Select(x => x.Annual).OrderBy(v => v).ToList();
                return new SalaryTechnologyDto(
                    g.Key,
                    Percentile(values, 50),
                    Percentile(values, 25),
                    Percentile(values, 75),
                    values.Count,
                    currency);
            })
            .OrderByDescending(t => t.MedianAnnual)
            .ThenBy(t => t.Technology)
            .Take(request.MaxTechnologies)
            .ToList();

        return new SalarySummaryDto(currency, coverage, technologies, byCurrency, request.MinimumSamples, DisclaimerText);
    }

    private static string ResolveCurrency(string? requested, IReadOnlyList<SalaryCurrencyOptionDto> available)
    {
        if (!string.IsNullOrWhiteSpace(requested))
            return requested.Trim().ToUpperInvariant();

        return available.Count > 0 ? available[0].Currency : "USD";
    }

    private static (decimal Annual, IReadOnlyList<string> Technologies) ToAnnualMidpoint(JobSalaryEntry entry)
    {
        var period = SalaryRange.ParsePeriod(entry.Period);
        var min = SalaryRange.ToAnnualValue(entry.Min, period);
        var max = entry.Max is null ? min : SalaryRange.ToAnnualValue(entry.Max.Value, period);
        var midpoint = max >= min ? (min + max) / 2m : min;
        return (Math.Round(midpoint, 2), entry.Technologies);
    }

    /// <summary>Percentil com interpolação linear sobre a lista já ordenada.</summary>
    public static decimal Percentile(List<decimal> sorted, int percentile)
    {
        if (sorted.Count == 0)
            return 0m;
        if (sorted.Count == 1)
            return sorted[0];

        var position = (sorted.Count - 1) * percentile / 100m;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return Math.Round(sorted[lower], 2);

        var weight = position - lower;
        var value = sorted[lower] + (sorted[upper] - sorted[lower]) * weight;
        return Math.Round(value, 2);
    }
}
