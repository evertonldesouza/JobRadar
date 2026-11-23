using JobRadar.Application.DTOs;
using JobRadar.Application.UseCases.Salaries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SalariesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalariesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Comparativo salarial por tecnologia (mediana, p25 e p75 em base anual).
    /// Apenas vagas que informam salário entram na conta — a cobertura vem no payload.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SalarySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? currency,
        [FromQuery] int minimumSamples = 3,
        [FromQuery] int maxTechnologies = 12)
    {
        var result = await _mediator.Send(new GetSalarySummaryQuery(currency, minimumSamples, maxTechnologies));
        return Ok(result);
    }
}
