using FluentValidation;
using JobRadar.Application.UseCases.Favorites;

namespace JobRadar.Application.Validators;

public sealed class MergeFavoritesCommandValidator : AbstractValidator<MergeFavoritesCommand>
{
    /// <summary>Limite por sincronização — evita payloads abusivos vindos do cliente.</summary>
    public const int MaxFavoritesPerMerge = 200;

    public MergeFavoritesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Usuário é obrigatório.");

        RuleFor(x => x.JobIds)
            .NotNull().WithMessage("Lista de favoritos é obrigatória.")
            .Must(ids => ids is null || ids.Count <= MaxFavoritesPerMerge)
            .WithMessage($"No máximo {MaxFavoritesPerMerge} favoritos por sincronização.");
    }
}
