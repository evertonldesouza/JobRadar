using JobRadar.Domain.Entities;
using JobRadar.Infrastructure.Services;

namespace JobRadar.Tests.Domain;

public class EmailTemplatesTests
{
    [Fact]
    public void JobAlert_EscapesHtmlInJobFields()
    {
        var user = User.CreateWithPassword("Ana", "ana@exemplo.com", "hash");
        var job = Job.Create("<script>alert(1)</script>", "Co & Filhos", "Remote",
            "https://example.com/vaga?a=1&b=2", DateTime.UtcNow, ["React"], "Test");
        var alert = JobAlert.Create(user.Id, "React", null, null);

        var (subject, _, html) = EmailTemplates.JobAlert(user, job, alert);

        Assert.Contains("Nova vaga", subject);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("Co &amp; Filhos", html);
    }

    [Fact]
    public void PasswordReset_ContainsResetUrl()
    {
        var user = User.CreateWithPassword("Ana", "ana@exemplo.com", "hash");

        var (_, text, html) = EmailTemplates.PasswordReset(user, "https://example.com/reset#token=abc");

        Assert.Contains("https://example.com/reset#token=abc", text);
        Assert.Contains("Criar nova senha", html);
    }
}
