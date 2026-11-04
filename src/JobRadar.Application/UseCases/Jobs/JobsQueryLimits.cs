namespace JobRadar.Application.UseCases.Jobs;

/// <summary>Limites públicos da listagem de vagas (também usados na validação e no export).</summary>
public static class JobsQueryLimits
{
    public const int MaxPageSize = 100;
    public const int MaxExportItems = 5_000;
}
