using System.Text;
using JobRadar.Application.DTOs;
using JobRadar.Application.UseCases.Jobs;
using JobRadar.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<JobsController> _logger;
    private readonly IReadOnlyList<string> _sources;

    public JobsController(IMediator mediator, ILogger<JobsController> logger, IEnumerable<IJobSource> sources)
    {
        _mediator = mediator;
        _logger = logger;
        _sources = sources.Select(s => s.Name).OrderBy(n => n).ToList();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? technology,
        [FromQuery] string? location,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetJobsQuery(technology, location, source, page, pageSize));
        return Ok(result);
    }

    [HttpGet("sources")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public IActionResult GetSources() => Ok(_sources);

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(
        [FromQuery] string? technology,
        [FromQuery] string? location,
        [FromQuery] string? source)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Título,Empresa,Localização,Url,Publicação,Fonte,Tecnologias");

        var exported = 0;
        var page = 1;
        while (exported < JobsQueryLimits.MaxExportItems)
        {
            var result = await _mediator.Send(new GetJobsQuery(
                technology, location, source, page, JobsQueryLimits.MaxPageSize));

            foreach (var job in result.Items)
            {
                var techs = string.Join("; ", job.Technologies);
                csv.AppendLine(string.Join(",",
                    Csv(job.Title), Csv(job.Company), Csv(job.Location), Csv(job.Url),
                    Csv(job.PublishedAt.ToString("yyyy-MM-dd")), Csv(job.Source), Csv(techs)));
                exported++;
            }

            if (!result.HasNextPage || result.Items.Count() == 0)
                break;
            page++;
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"vagas-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>Escapa um campo CSV (aspas RFC 4180 + proteção contra CSV injection).</summary>
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        var safe = value.TrimStart();
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            safe = "'" + safe;

        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }

    [HttpPost("sync")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Sync()
    {
        var count = await _mediator.Send(new SyncJobsCommand());
        _logger.LogInformation("Sincronização manual concluída: {Count} vagas importadas", count);
        return Ok(new { imported = count });
    }

    [HttpDelete("duplicates")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveDuplicates()
    {
        var count = await _mediator.Send(new RemoveDuplicatesCommand());
        _logger.LogInformation("Duplicatas removidas: {Count}", count);
        return Ok(new { removed = count });
    }
}
