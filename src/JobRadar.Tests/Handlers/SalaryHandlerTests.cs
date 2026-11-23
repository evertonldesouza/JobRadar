using JobRadar.Application.UseCases.Salaries;
using JobRadar.Domain.Interfaces;
using JobRadar.Domain.ValueObjects;
using Moq;

namespace JobRadar.Tests.Handlers;

public class SalaryHandlerTests
{
    private static JobSalaryEntry Entry(decimal min, decimal? max, string currency, string period, params string[] technologies) =>
        new(technologies, min, max, currency, period);

    private static Mock<IJobRepository> RepoWith(IReadOnlyList<JobSalaryEntry> entries, int totalJobs)
    {
        var repo = new Mock<IJobRepository>();
        repo.Setup(r => r.GetSalaryEntriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entries);
        repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(totalJobs);
        return repo;
    }

    [Fact]
    public async Task Summary_ComputesMedianInAnnualBase()
    {
        // Python: 60k, 80k e 100k anuais → mediana 80k.
        var repo = RepoWith([
            Entry(60000, null, "USD", "year", "python"),
            Entry(80000, null, "USD", "year", "python"),
            Entry(100000, null, "USD", "year", "python")
        ], totalJobs: 10);

        var handler = new GetSalarySummaryQueryHandler(repo.Object);
        var result = await handler.Handle(new GetSalarySummaryQuery(), default);

        var python = Assert.Single(result.Technologies);
        Assert.Equal("python", python.Technology);
        Assert.Equal(80000, python.MedianAnnual);
        Assert.Equal(3, python.Samples);
        Assert.Equal("USD", python.Currency);
    }

    [Fact]
    public async Task Summary_ConvertsMonthlyValuesToAnnual()
    {
        var repo = RepoWith([
            Entry(5000, 5000, "USD", "month", "go"),
            Entry(5000, 5000, "USD", "month", "go"),
            Entry(5000, 5000, "USD", "month", "go")
        ], totalJobs: 3);

        var result = await new GetSalarySummaryQueryHandler(repo.Object)
            .Handle(new GetSalarySummaryQuery(), default);

        Assert.Equal(60000, Assert.Single(result.Technologies).MedianAnnual);
    }

    [Fact]
    public async Task Summary_FiltersTechnologiesBelowMinimumSamples()
    {
        var repo = RepoWith([
            Entry(90000, null, "USD", "year", "python"),
            Entry(95000, null, "USD", "year", "python"),
            Entry(120000, null, "USD", "year", "rust") // só 1 amostra
        ], totalJobs: 5);

        var result = await new GetSalarySummaryQueryHandler(repo.Object)
            .Handle(new GetSalarySummaryQuery(MinimumSamples: 2), default);

        var technology = Assert.Single(result.Technologies);
        Assert.Equal("python", technology.Technology);
    }

    [Fact]
    public async Task Summary_PicksCurrencyWithMostSamplesWhenNotSpecified()
    {
        var repo = RepoWith([
            Entry(80000, null, "USD", "year", "python"),
            Entry(90000, null, "USD", "year", "python"),
            Entry(70000, null, "GBP", "year", "python")
        ], totalJobs: 3);

        var result = await new GetSalarySummaryQueryHandler(repo.Object)
            .Handle(new GetSalarySummaryQuery(MinimumSamples: 1), default);

        Assert.Equal("USD", result.Currency);
        Assert.Equal(85000, Assert.Single(result.Technologies).MedianAnnual);
    }

    [Fact]
    public async Task Summary_ReportsCoverageHonestly()
    {
        var repo = RepoWith([
            Entry(80000, null, "USD", "year", "python"),
            Entry(90000, null, "USD", "year", "python")
        ], totalJobs: 200);

        var result = await new GetSalarySummaryQueryHandler(repo.Object)
            .Handle(new GetSalarySummaryQuery(MinimumSamples: 1), default);

        Assert.Equal(2, result.Coverage.JobsWithSalary);
        Assert.Equal(200, result.Coverage.TotalJobs);
        Assert.Equal(1.0m, result.Coverage.Percentage);
        Assert.Contains("somente", result.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Summary_IgnoresUnrealisticAnnualValues()
    {
        // 20/ano e 5.000.000/ano ficam fora da faixa de sanidade.
        var repo = RepoWith([
            Entry(20, null, "USD", "year", "python"),
            Entry(5_000_000, null, "USD", "year", "python"),
            Entry(90000, null, "USD", "year", "python")
        ], totalJobs: 3);

        var result = await new GetSalarySummaryQueryHandler(repo.Object)
            .Handle(new GetSalarySummaryQuery(MinimumSamples: 1), default);

        var python = Assert.Single(result.Technologies);
        Assert.Equal(1, python.Samples);
        Assert.Equal(90000, python.MedianAnnual);
    }

    [Theory]
    [InlineData(new double[] { 10, 20, 30 }, 50, 20)]
    [InlineData(new double[] { 10, 20, 30 }, 25, 15)]
    [InlineData(new double[] { 10, 20, 30 }, 75, 25)]
    [InlineData(new double[] { 40 }, 50, 40)]
    public void Percentile_Interpolates(double[] values, int percentile, double expected)
    {
        var sorted = values.Select(v => (decimal)v).ToList();

        Assert.Equal((decimal)expected, GetSalarySummaryQueryHandler.Percentile(sorted, percentile));
    }
}
