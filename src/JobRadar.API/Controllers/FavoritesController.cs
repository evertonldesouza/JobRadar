using JobRadar.Application.UseCases.Favorites;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> GetFavorites()
    {
        var favorites = await _mediator.Send(new GetFavoritesQuery(GetUserId()));
        return Ok(favorites);
    }

    [HttpPost("{jobId}")]
    public async Task<IActionResult> AddFavorite(Guid jobId)
    {
        try
        {
            await _mediator.Send(new AddFavoriteCommand(GetUserId(), jobId));
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{jobId}")]
    public async Task<IActionResult> RemoveFavorite(Guid jobId)
    {
        await _mediator.Send(new RemoveFavoriteCommand(GetUserId(), jobId));
        return NoContent();
    }
}