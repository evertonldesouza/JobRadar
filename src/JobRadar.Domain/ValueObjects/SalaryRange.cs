namespace JobRadar.Domain.ValueObjects;

/// <summary>Período a que a faixa salarial se refere.</summary>
public enum SalaryPeriod
{
    Year = 0,
    Month = 1,
    Week = 2,
    Day = 3,
    Hour = 4
}

/// <summary>
/// Faixa salarial informada pela fonte do anúncio. Valores ficam na moeda e no
/// período originais; a conversão para base anual é feita sob demanda.
/// </summary>
public sealed record SalaryRange
{
    public decimal Min { get; }
    public decimal? Max { get; }
    public string Currency { get; }
    public SalaryPeriod Period { get; }

    public SalaryRange(decimal min, decimal? max, string currency, SalaryPeriod period)
    {
        if (min < 0)
            throw new ArgumentOutOfRangeException(nameof(min), "Salário mínimo não pode ser negativo.");

        Min = min;
        Max = max is not null && max >= min ? max : null;

        var code = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length != 3)
            throw new ArgumentException("Moeda deve usar o código ISO de 3 letras (ex.: USD).", nameof(currency));

        Currency = code;
        Period = period;
    }

    /// <summary>Equivalente anual do piso (52 semanas, 260 dias úteis, 2.080 horas).</summary>
    public decimal AnnualMin => ToAnnualValue(Min, Period);

    /// <summary>Equivalente anual do teto, quando informado.</summary>
    public decimal? AnnualMax => Max is null ? null : ToAnnualValue(Max.Value, Period);

    /// <summary>Converte um valor do período informado para o equivalente anual.</summary>
    public static decimal ToAnnualValue(decimal value, SalaryPeriod period) => period switch
    {
        SalaryPeriod.Year => value,
        SalaryPeriod.Month => value * 12,
        SalaryPeriod.Week => value * 52,
        SalaryPeriod.Day => value * 260,
        SalaryPeriod.Hour => value * 2080,
        _ => value
    };

    public static SalaryPeriod ParsePeriod(string? value) => (value ?? string.Empty).ToLowerInvariant() switch
    {
        "month" => SalaryPeriod.Month,
        "week" => SalaryPeriod.Week,
        "day" => SalaryPeriod.Day,
        "hour" => SalaryPeriod.Hour,
        _ => SalaryPeriod.Year
    };

    public static string PeriodToString(SalaryPeriod period) => period switch
    {
        SalaryPeriod.Month => "month",
        SalaryPeriod.Week => "week",
        SalaryPeriod.Day => "day",
        SalaryPeriod.Hour => "hour",
        _ => "year"
    };
}
