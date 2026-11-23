using JobRadar.Domain.Entities;
using JobRadar.Domain.ValueObjects;

namespace JobRadar.Domain.Interfaces;

public interface IJobRepository
{
    Task<(IEnumerable<Job> Jobs, int TotalCount)> GetAllAsync(
        string? technology = null,
        string? location = null,
        string? source = null,
        int page = 1,
        int pageSize = 20);
    Task<Job?> GetByIdAsync(Guid id);
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Vagas que informam salário, já projetadas para as estatísticas.</summary>
    Task<IReadOnlyList<JobSalaryEntry>> GetSalaryEntriesAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByUrlAsync(string url);
    Task AddRangeAsync(IEnumerable<Job> jobs);

    /// <summary>Persiste alterações pendentes em vagas já rastreadas (ex.: backfill de salário).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByTitleAndCompanyAsync(string title, string company);
    Task<int> RemoveDuplicatesAsync();

    /// <summary>
    /// Remove vagas publicadas antes do limite informado. Anúncios antigos não
    /// são mais candidaturas úteis e, mantidos, ocupam espaço e poluem a listagem.
    /// Favoritos dessas vagas são removidos em cascata.
    /// </summary>
    Task<int> RemoveOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default);

    /// <summary>Devolve, em uma única consulta, quais dos ids informados existem.</summary>
    Task<IReadOnlyList<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carrega as vagas (rastreadas) cujas URLs estão na lista. Usado para
    /// completar dados que a fonte passou a informar depois da primeira importação.
    /// </summary>
    Task<IReadOnlyList<Job>> GetByUrlsAsync(IReadOnlyCollection<string> urls, CancellationToken cancellationToken = default);

    /// <summary>Devolve, em uma única consulta, quais das URLs informadas já existem.</summary>
    Task<IReadOnlyList<string>> GetExistingUrlsAsync(IReadOnlyCollection<string> urls, CancellationToken cancellationToken = default);

    /// <summary>Devolve, em uma única consulta, as chaves título|empresa (minúsculas) já existentes.</summary>
    Task<IReadOnlyList<string>> GetExistingDeduplicationKeysAsync(IReadOnlyCollection<string> titles, CancellationToken cancellationToken = default);
}
