using System.Globalization;
using System.Text.RegularExpressions;
using JobRadar.Domain.ValueObjects;

namespace JobRadar.Application.Common;

/// <summary>
/// Interpreta a faixa salarial quando a fonte a informa como texto livre
/// (ex.: <c>"$20k -$35k"</c>, <c>"$3.5k–$5k/month"</c>, <c>"R$ 8.000 - R$ 12.000"</c>).
/// Retorna <c>null</c> quando não há salário confiável — nunca inventa valores.
/// </summary>
public static partial class SalaryParser
{
    private const decimal MaxReasonableAmount = 10_000_000m;

    private static readonly Dictionary<string, string> SymbolCurrencies = new(StringComparer.Ordinal)
    {
        ["R$"] = "BRL",
        ["€"] = "EUR",
        ["£"] = "GBP",
        ["₹"] = "INR",
        ["$"] = "USD"
    };

    private static readonly HashSet<string> KnownCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "BRL", "CAD", "AUD", "INR", "CHF", "PLN",
        "MXN", "ARS", "CLP", "COP", "SEK", "NOK", "DKK", "JPY", "CNY", "NZD", "SGD", "ZAR", "CZK"
    };

    [GeneratedRegex(@"\b([A-Z]{3})\b")]
    private static partial Regex CurrencyCodeRegex();

    [GeneratedRegex(@"(\d[\d.,]*)\s*([kK])?")]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"(?:/|per\s+|por\s+|an\s+|a\s+)(h|hr|hrs|hour|hours|day|days|week|weeks|mo|month|months|yr|year|years|annum|mês|mes|meses|dia|dias|semana|semanas|hora|horas)\b")]
    private static partial Regex PeriodRegex();

    [GeneratedRegex(@"\b(monthly|mensal|hourly|weekly|semanal|annually|annual|anual|yearly)\b")]
    private static partial Regex PeriodWordRegex();

    /// <summary>Converte o texto da fonte em uma faixa salarial, ou <c>null</c> se não houver dados confiáveis.</summary>
    public static SalaryRange? TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var text = raw.Trim();
        if (text.Length > 120)
            return null;

        var lower = text.ToLowerInvariant();
        if (lower.Contains("competitive") || lower.Contains("negotiable") || lower.Contains("a combinar"))
            return null;

        var currency = DetectCurrency(text);
        if (currency is null)
            return null;

        var amounts = AmountRegex()
            .Matches(text)
            .Select(m => ParseAmount(m.Groups[1].Value, m.Groups[2].Success))
            .Where(value => value is > 0 and <= MaxReasonableAmount)
            .ToList();

        if (amounts.Count == 0)
            return null;

        var min = amounts.Min();
        var max = amounts.Count > 1 ? amounts.Max() : (decimal?)null;
        var period = DetectPeriod(lower);

        return max is not null && max == min
            ? new SalaryRange(min, null, currency, period)
            : new SalaryRange(min, max, currency, period);
    }

    private static string? DetectCurrency(string text)
    {
        foreach (Match match in CurrencyCodeRegex().Matches(text.ToUpperInvariant()))
        {
            if (KnownCurrencies.Contains(match.Groups[1].Value))
                return match.Groups[1].Value.ToUpperInvariant();
        }

        foreach (var (symbol, code) in SymbolCurrencies)
        {
            if (text.Contains(symbol, StringComparison.Ordinal))
                return code;
        }

        return null;
    }

    private static SalaryPeriod DetectPeriod(string lower)
    {
        var match = PeriodRegex().Match(lower);
        var token = match.Success
            ? match.Groups[1].Value
            : PeriodWordRegex().Match(lower) is { Success: true } word ? word.Groups[1].Value : string.Empty;

        return token switch
        {
            "h" or "hr" or "hrs" or "hour" or "hours" or "hora" or "horas" or "hourly" => SalaryPeriod.Hour,
            "day" or "days" or "dia" or "dias" => SalaryPeriod.Day,
            "week" or "weeks" or "semana" or "semanas" or "weekly" or "semanal" => SalaryPeriod.Week,
            "mo" or "month" or "months" or "mês" or "mes" or "meses" or "monthly" or "mensal" => SalaryPeriod.Month,
            _ => SalaryPeriod.Year
        };
    }

    /// <summary>Lê um número escrito em formato en-US, pt-BR ou abreviado com "k".</summary>
    public static decimal ParseAmount(string numberPart, bool thousandsSuffix)
    {
        var raw = numberPart.Trim().Trim('.', ',');
        if (raw.Length == 0)
            return 0m;

        var hasDot = raw.Contains('.');
        var hasComma = raw.Contains(',');

        string normalized;

        if (hasDot && hasComma)
        {
            // O último separador é o decimal: "1,234.56" ou "1.234,56".
            var lastDot = raw.LastIndexOf('.');
            var lastComma = raw.LastIndexOf(',');
            normalized = lastDot > lastComma
                ? raw.Replace(",", string.Empty)
                : raw.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (hasComma)
        {
            normalized = LooksLikeThousands(raw, ',')
                ? raw.Replace(",", string.Empty)
                : raw.Replace(',', '.');
        }
        else if (hasDot)
        {
            normalized = !thousandsSuffix && LooksLikeThousands(raw, '.')
                ? raw.Replace(".", string.Empty)
                : raw;
        }
        else
        {
            normalized = raw;
        }

        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return 0m;

        return thousandsSuffix ? value * 1000m : value;
    }

    /// <summary>"8.000" e "60,000" são milhares; "3.5" e "3,50" são decimais.</summary>
    private static bool LooksLikeThousands(string raw, char separator)
    {
        var groups = raw.Split(separator);
        if (groups.Length < 2)
            return false;

        return groups[0].Length is >= 1 and <= 3
            && groups.Skip(1).All(g => g.Length == 3);
    }
}
