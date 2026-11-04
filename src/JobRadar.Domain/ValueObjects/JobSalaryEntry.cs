namespace JobRadar.Domain.ValueObjects;

/// <summary>
/// Amostra salarial enxuta usada nas estatísticas (sem carregar a vaga inteira).
/// </summary>
public sealed record JobSalaryEntry(
    IReadOnlyList<string> Technologies,
    decimal Min,
    decimal? Max,
    string Currency,
    string Period);
