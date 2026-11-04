using JobRadar.API.Extensions;
using JobRadar.Application.DTOs;
using JobRadar.Application.UseCases.Alerts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AlertsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var alerts = await _mediator.Send(new GetAlertsQuery(this.User.GetUserId()));
        return Ok(alerts);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateAlertRequest request)
    {
        var alert = await _mediator.Send(new CreateAlertCommand(this.User.GetUserId(), request.Technology, request.Location, request.Source));
        return CreatedAtAction(nameof(GetAll), new { id = alert.Id }, alert);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteAlertCommand(id, this.User.GetUserId()));
        return NoContent();
    }
}

public record CreateAlertRequest(string? Technology, string? Location, string? Source);
