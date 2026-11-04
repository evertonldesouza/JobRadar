using FluentValidation;
using JobRadar.Application.UseCases.Alerts;

namespace JobRadar.Application.Validators;

public sealed class CreateAlertCommandValidator : AbstractValidator<CreateAlertCommand>
{
    public CreateAlertCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Usuário é obrigatório.");

        RuleFor(x => x.Technology).MaximumLength(100).WithMessage("Tecnologia deve ter no máximo 100 caracteres.");
        RuleFor(x => x.Location).MaximumLength(200).WithMessage("Localização deve ter no máximo 200 caracteres.");
        RuleFor(x => x.Source).MaximumLength(50).WithMessage("Fonte deve ter no máximo 50 caracteres.");
    }
}
