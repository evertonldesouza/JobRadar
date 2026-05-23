using System.Text;
using JobRadar.Application.UseCases.Jobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IMediator mediator, ILogger<JobsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Application.DTOs.PagedResult<Application.DTOs.JobDto>), StatusCodes.Status200OK)]
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
    public IActionResult GetSources()
    {
        return Ok(new[] { "RemoteOK", "Remotive", "CareerNest", "JobsBase", "Adzuna" });
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] string? technology,
        [FromQuery] string? location,
        [FromQuery] string? source)
    {
        var result = await _mediator.Send(new GetJobsQuery(technology, location, source, 1, 5000));
        var csv = new StringBuilder();
        csv.AppendLine("Título,Empresa,Localização,Url,Publicação,Source,Tecnologias");
        foreach (var job in result.Items)
        {
            var techs = string.Join("; ", job.Technologies);
            csv.AppendLine($"\"{job.Title}\",\"{job.Company}\",\"{job.Location}\",{job.Url},{job.PublishedAt:yyyy-MM-dd},{job.Source},\"{techs}\"");
        }
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"vagas-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync()
    {
        var count = await _mediator.Send(new SyncJobsCommand());
        _logger.LogInformation("Sincronização concluída: {Count} vagas importadas", count);
        return Ok(new { imported = count });
    }

    [HttpDelete("duplicates")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveDuplicates()
    {
        var count = await _mediator.Send(new RemoveDuplicatesCommand());
        _logger.LogInformation("Duplicatas removidas: {Count}", count);
        return Ok(new { removed = count });
    }
}