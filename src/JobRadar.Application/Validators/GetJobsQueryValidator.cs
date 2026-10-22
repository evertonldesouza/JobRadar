using FluentValidation;
using JobRadar.Application.UseCases.Jobs;

namespace JobRadar.Application.Validators;

public sealed class GetJobsQueryValidator : AbstractValidator<GetJobsQuery>
{
    public GetJobsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Página deve ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, JobsQueryLimits.MaxPageSize)
            .WithMessage($"Tamanho da página deve estar entre 1 e {JobsQueryLimits.MaxPageSize}.");

        RuleFor(x => x.Technology).MaximumLength(100).WithMessage("Tecnologia deve ter no máximo 100 caracteres.");
        RuleFor(x => x.Location).MaximumLength(200).WithMessage("Localização deve ter no máximo 200 caracteres.");
        RuleFor(x => x.Source).MaximumLength(100).WithMessage("Fonte deve ter no máximo 100 caracteres.");
    }
}
