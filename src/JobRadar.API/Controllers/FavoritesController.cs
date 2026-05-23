using JobRadar.Application.UseCases.Favorites;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Application.DTOs.JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFavorites()
    {
        var favorites = await _mediator.Send(new GetFavoritesQuery(GetUserId()));
        return Ok(favorites);
    }

    [HttpPost("{jobId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFavorite(Guid jobId)
    {
        await _mediator.Send(new AddFavoriteCommand(GetUserId(), jobId));
        _logger.LogInformation("Favorito adicionado: User={UserId}, Job={JobId}", GetUserId(), jobId);
        return NoContent();
    }

    [HttpDelete("{jobId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFavorite(Guid jobId)
    {
        await _mediator.Send(new RemoveFavoriteCommand(GetUserId(), jobId));
        _logger.LogInformation("Favorito removido: User={UserId}, Job={JobId}", GetUserId(), jobId);
        return NoContent();
    }
}