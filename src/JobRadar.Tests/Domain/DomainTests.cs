using JobRadar.Domain.Entities;

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
}
