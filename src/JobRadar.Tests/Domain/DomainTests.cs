using JobRadar.Domain.Entities;
using JobRadar.Domain.ValueObjects;

namespace JobRadar.Tests.Domain;

public class DomainTests
{
    [Fact]
    public void Job_Create_TrimsAndDeduplicatesTechnologies()
    {
        var job = Job.Create(" Dev ", " Co ", " Remote ", " https://example.com/vaga ",
            DateTime.UtcNow, [" React ", "react", "", "Node"], "RemoteOK");

        Assert.Equal("Dev", job.Title);
        Assert.Equal("https://example.com/vaga", job.Url);
        Assert.Equal(["React", "Node"], job.Technologies);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-url")]
    [InlineData("ftp://example.com/vaga")]
    public void Job_Create_WithInvalidUrl_Throws(string url)
    {
        Assert.Throws<ArgumentException>(() =>
            Job.Create("Dev", "Co", "Remote", url, DateTime.UtcNow, [], "Test"));
    }

    [Fact]
    public void User_CreateWithPassword_NormalizesEmail()
    {
        var user = User.CreateWithPassword(" Ana ", "  ANA@Exemplo.COM ", "hash");

        Assert.Equal("Ana", user.Name);
        Assert.Equal("ana@exemplo.com", user.Email);
    }

    [Fact]
    public void User_LinkGoogleAccount_SetsGoogleId()
    {
        var user = User.CreateWithPassword("Ana", "ana@exemplo.com", "hash");

        user.LinkGoogleAccount("google-123");

        Assert.Equal("google-123", user.GoogleId);
    }

    [Fact]
    public void JobAlert_Create_NormalizesBlankFiltersToNull()
    {
        var alert = JobAlert.Create(Guid.NewGuid(), "  ", " Remoto ", null);

        Assert.Null(alert.Technology);
        Assert.Equal("Remoto", alert.Location);
    }

    [Fact]
    public void TrySetSalary_FillsWhenMissing()
    {
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/1", DateTime.UtcNow, ["C#"], "Test");
        Assert.False(job.HasSalary);

        var changed = job.TrySetSalary(new SalaryRange(80000, 100000, "USD", SalaryPeriod.Year));

        Assert.True(changed);
        Assert.True(job.HasSalary);
        Assert.Equal(80000, job.SalaryMin);
        Assert.Equal("USD", job.SalaryCurrency);
        Assert.Equal("year", job.SalaryPeriod);
    }

    [Fact]
    public void TrySetSalary_DoesNotOverwriteKnownSalary()
    {
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/2", DateTime.UtcNow, ["Go"], "Test",
            new SalaryRange(50000, 60000, "USD", SalaryPeriod.Year));

        var changed = job.TrySetSalary(new SalaryRange(90000, 99000, "EUR", SalaryPeriod.Month));

        Assert.False(changed);
        Assert.Equal(50000, job.SalaryMin);
        Assert.Equal("USD", job.SalaryCurrency);
    }

    [Fact]
    public void TrySetSalary_WithNull_DoesNothing()
    {
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/3", DateTime.UtcNow, ["Go"], "Test");

        Assert.False(job.TrySetSalary(null));
        Assert.False(job.HasSalary);
    }

    [Fact]
    public void SalaryRange_FromJob_RoundTrips()
    {
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/4", DateTime.UtcNow, ["Go"], "Test",
            new SalaryRange(4000, 5000, "EUR", SalaryPeriod.Month));

        var range = SalaryRange.FromJob(job);

        Assert.NotNull(range);
        Assert.Equal(4000, range!.Min);
        Assert.Equal(5000, range.Max);
        Assert.Equal("EUR", range.Currency);
        Assert.Equal(SalaryPeriod.Month, range.Period);
    }

    [Fact]
    public void SalaryRange_FromJob_WithoutSalary_ReturnsNull()
    {
        var job = Job.Create("Dev", "Co", "Remote", "https://example.com/5", DateTime.UtcNow, [], "Test");

        Assert.Null(SalaryRange.FromJob(job));
    }
}
