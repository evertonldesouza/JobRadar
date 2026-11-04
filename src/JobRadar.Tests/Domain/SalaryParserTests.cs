using JobRadar.Application.Common;
using JobRadar.Domain.ValueObjects;

namespace JobRadar.Tests.Domain;

public class SalaryParserTests
{
    [Theory]
    [InlineData("$20k -$35k", 20000, 35000, "USD", SalaryPeriod.Year)]
    [InlineData("$3.5k–$5k/month", 3500, 5000, "USD", SalaryPeriod.Month)]
    [InlineData("€60,000 - €80,000", 60000, 80000, "EUR", SalaryPeriod.Year)]
    [InlineData("R$ 8.000 - R$ 12.000", 8000, 12000, "BRL", SalaryPeriod.Year)]
    [InlineData("£40k - £55k per annum", 40000, 55000, "GBP", SalaryPeriod.Year)]
    [InlineData("$50/hour", 50, 50, "USD", SalaryPeriod.Hour)]
    [InlineData("USD 100,000", 100000, 100000, "USD", SalaryPeriod.Year)]
    [InlineData("R$ 150 por hora", 150, 150, "BRL", SalaryPeriod.Hour)]
    public void TryParse_WithKnownFormats_ExtractsRange(
        string raw, decimal expectedMin, decimal expectedMax, string expectedCurrency, SalaryPeriod expectedPeriod)
    {
        var result = SalaryParser.TryParse(raw);

        Assert.NotNull(result);
        Assert.Equal(expectedMin, result!.Min);
        // Valor único vira um ponto (sem teto); faixa só quando há dois valores distintos.
        Assert.Equal(expectedMax == expectedMin ? null : expectedMax, result.Max);
        Assert.Equal(expectedCurrency, result.Currency);
        Assert.Equal(expectedPeriod, result.Period);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Competitive")]
    [InlineData("Negotiable salary")]
    [InlineData("A combinar")]
    [InlineData("50k - 60k")] // sem moeda: não dá para comparar com segurança
    public void TryParse_WithoutReliableData_ReturnsNull(string? raw)
    {
        Assert.Null(SalaryParser.TryParse(raw));
    }

    [Fact]
    public void TryParse_WithSingleValue_LeavesMaxEmpty()
    {
        var result = SalaryParser.TryParse("$120k");

        Assert.NotNull(result);
        Assert.Equal(120000, result!.Min);
        Assert.Null(result.Max);
    }

    [Theory]
    [InlineData("3.5", false, 3.5)]
    [InlineData("8.000", false, 8000)]
    [InlineData("60,000", false, 60000)]
    [InlineData("1,234.56", false, 1234.56)]
    [InlineData("1.234,56", false, 1234.56)]
    [InlineData("20", true, 20000)]
    public void ParseAmount_HandlesBothDecimalSeparators(string raw, bool kSuffix, double expected)
    {
        Assert.Equal((decimal)expected, SalaryParser.ParseAmount(raw, kSuffix));
    }
}

public class SalaryRangeTests
{
    [Theory]
    [InlineData(60000, SalaryPeriod.Year, 60000)]
    [InlineData(5000, SalaryPeriod.Month, 60000)]
    [InlineData(1000, SalaryPeriod.Week, 52000)]
    [InlineData(250, SalaryPeriod.Day, 65000)]
    [InlineData(50, SalaryPeriod.Hour, 104000)]
    public void ToAnnualValue_NormalizesPeriods(decimal value, SalaryPeriod period, decimal expectedAnnual)
    {
        Assert.Equal(expectedAnnual, SalaryRange.ToAnnualValue(value, period));
    }

    [Fact]
    public void Constructor_WithMaxLowerThanMin_DiscardsMax()
    {
        var range = new SalaryRange(80000, 60000, "USD", SalaryPeriod.Year);

        Assert.Null(range.Max);
    }

    [Fact]
    public void Constructor_WithNegativeMin_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryRange(-1, null, "USD", SalaryPeriod.Year));
    }

    [Fact]
    public void Constructor_WithInvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SalaryRange(1000, null, "US", SalaryPeriod.Year));
    }
}
