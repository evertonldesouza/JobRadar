using JobRadar.API.Extensions;
using JobRadar.Application.DTOs;
using JobRadar.Application.UseCases.Favorites;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(IMediator mediator, ILogger<FavoritesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFavorites()
    {
        var favorites = await _mediator.Send(new GetFavoritesQuery(this.User.GetUserId()));
        return Ok(favorites);
    }

    /// <summary>
    /// Sincroniza favoritos salvos localmente (visitante) com a conta.
    /// Idempotente: repetir a chamada não duplica nem gera erro.
    /// </summary>
    [HttpPost("merge")]
    [ProducesResponseType(typeof(IEnumerable<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MergeFavorites([FromBody] MergeFavoritesRequest request)
    {
        var userId = this.User.GetUserId();
        var favorites = await _mediator.Send(new MergeFavoritesCommand(userId, request?.JobIds ?? []));
        _logger.LogInformation(
            "Favoritos sincronizados: User={UserId}, Recebidos={Count}", userId, request?.JobIds?.Count ?? 0);
        return Ok(favorites);
    }

    [HttpPost("{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFavorite(Guid jobId)
    {
        var userId = this.User.GetUserId();
        await _mediator.Send(new AddFavoriteCommand(userId, jobId));
        _logger.LogInformation("Favorito adicionado: User={UserId}, Job={JobId}", userId, jobId);
        return NoContent();
    }

    [HttpDelete("{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFavorite(Guid jobId)
    {
        var userId = this.User.GetUserId();
        await _mediator.Send(new RemoveFavoriteCommand(userId, jobId));
        _logger.LogInformation("Favorito removido: User={UserId}, Job={JobId}", userId, jobId);
        return NoContent();
    }
}

/// <summary>Payload da sincronização de favoritos vindos do navegador.</summary>
public record MergeFavoritesRequest(List<Guid> JobIds);
