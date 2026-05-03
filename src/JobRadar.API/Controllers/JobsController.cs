using JobRadar.Application.UseCases.Jobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
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
    public async Task<IActionResult> Sync()
    {
        var count = await _mediator.Send(new SyncJobsCommand());
        return Ok(new { imported = count });
    }
}