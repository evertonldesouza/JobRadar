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
        [FromQuery] string? location)
    {
        var jobs = await _mediator.Send(new GetJobsQuery(technology, location));
        return Ok(jobs);
    }
}