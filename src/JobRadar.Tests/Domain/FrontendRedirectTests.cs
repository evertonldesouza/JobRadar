using JobRadar.Application.Common;

namespace JobRadar.Tests.Domain;

public class FrontendRedirectTests
{
    [Fact]
    public void Build_WithToken_PutsTokenInFragment()
    {
        var url = FrontendRedirect.BuildAuthRedirect("https://example.com/app", "tok.en");

        Assert.Equal("https://example.com/app/#token=tok.en", url);
    }

    [Fact]
    public void Build_WithError_PutsErrorInFragment()
    {
        var url = FrontendRedirect.BuildAuthRedirect("https://example.com/app/", null, error: "google");

        Assert.Equal("https://example.com/app/#authError=google", url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/caminho-relativo")]
    public void Build_WithInvalidBase_ReturnsNull(string? baseUrl)
    {
        Assert.Null(FrontendRedirect.BuildAuthRedirect(baseUrl, "token"));
    }
}
