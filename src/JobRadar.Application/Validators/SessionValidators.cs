using FluentValidation;
using JobRadar.Application.UseCases.Auth;

namespace JobRadar.Application.Validators;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token é obrigatório.")
            .MaximumLength(500).WithMessage("Refresh token inválido.");
    }
}

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(300).WithMessage("Email deve ter no máximo 300 caracteres.");
    }
}

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token é obrigatório.")
            .MaximumLength(500).WithMessage("Token inválido.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(100).WithMessage("Senha deve ter no máximo 100 caracteres.")
            .Matches("[A-Za-z]").WithMessage("Senha deve conter ao menos uma letra.")
            .Matches("[0-9]").WithMessage("Senha deve conter ao menos um número.");
    }
}
