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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetJobsQuery(technology, location, page, pageSize));
        return Ok(result);
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