using FluentValidation;
using JobRadar.Application.UseCases.Salaries;

namespace JobRadar.Application.Validators;

public sealed class GetSalarySummaryQueryValidator : AbstractValidator<GetSalarySummaryQuery>
{
    public GetSalarySummaryQueryValidator()
    {
        RuleFor(x => x.Currency)
            .Length(3).WithMessage("Moeda deve usar o código ISO de 3 letras (ex.: USD).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.MinimumSamples)
            .InclusiveBetween(1, 100).WithMessage("Amostra mínima deve estar entre 1 e 100.");

        RuleFor(x => x.MaxTechnologies)
            .InclusiveBetween(1, 50).WithMessage("Máximo de tecnologias deve estar entre 1 e 50.");
    }
}
