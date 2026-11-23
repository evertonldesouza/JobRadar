using JobRadar.Application.DTOs;
using JobRadar.Domain.Entities;
using JobRadar.Domain.ValueObjects;

namespace JobRadar.Tests.Domain;

public class JobDtoTests
{
    private static Job JobWith(SalaryRange? salary) =>
        Job.Create("Dev", "Co", "Remote", "https://example.com/dto", DateTime.UtcNow, ["C#"], "Test", salary);

    [Fact]
    public void SalaryText_WithRange_ShowsBothEnds()
    {
        var dto = JobDto.FromJob(JobWith(new SalaryRange(60000, 90000, "USD", SalaryPeriod.Year)));

        Assert.Contains("60.000", dto.SalaryText);
        Assert.Contains("90.000", dto.SalaryText);
        Assert.EndsWith("/ano", dto.SalaryText);
    }

    [Fact]
    public void SalaryText_WhenMinEqualsMax_ShowsSingleValue()
    {
        // Anúncios assim existem ("GBP 85.000 – 85.000"); repetir o valor confunde.
        var dto = JobDto.FromJob(JobWith(new SalaryRange(85000, 85000, "GBP", SalaryPeriod.Year)));

        Assert.Equal("GBP 85.000/ano", dto.SalaryText);
    }

    [Fact]
    public void SalaryText_WithoutSalary_IsNull()
    {
        Assert.Null(JobDto.FromJob(JobWith(null)).SalaryText);
    }
}
