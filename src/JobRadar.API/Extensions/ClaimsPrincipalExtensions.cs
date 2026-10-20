using System.Security.Claims;

namespace JobRadar.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Extrai o id do usuário do JWT; token sem a claim é tratado como não autenticado.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Token inválido: usuário não identificado.");
    }
}
