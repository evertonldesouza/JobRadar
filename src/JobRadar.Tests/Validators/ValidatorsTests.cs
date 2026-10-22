using JobRadar.Application.UseCases.Alerts;
using JobRadar.Application.UseCases.Auth;
using JobRadar.Application.UseCases.Jobs;
using JobRadar.Application.Validators;

namespace JobRadar.Tests.Validators;

public class ValidatorsTests
{
    [Fact]
    public void Register_WithWeakPassword_IsInvalid()
    {
        var validator = new RegisterCommandValidator();

        var result = validator.Validate(new RegisterCommand("Ana", "ana@exemplo.com", "curta"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Register_WithPasswordWithoutDigit_IsInvalid()
    {
        var validator = new RegisterCommandValidator();

        var result = validator.Validate(new RegisterCommand("Ana", "ana@exemplo.com", "somente-letras"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Register_WithValidData_IsValid()
    {
        var validator = new RegisterCommandValidator();

        var result = validator.Validate(new RegisterCommand("Ana", "ana@exemplo.com", "senha-forte-123"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetJobs_WithPageSizeAboveLimit_IsInvalid()
    {
        var validator = new GetJobsQueryValidator();

        var result = validator.Validate(new GetJobsQuery(null, null, null, 1, 5000));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateAlert_WithTooLongTechnology_IsInvalid()
    {
        var validator = new CreateAlertCommandValidator();

        var result = validator.Validate(new CreateAlertCommand(Guid.NewGuid(), new string('x', 101), null, null));

        Assert.False(result.IsValid);
    }
}
