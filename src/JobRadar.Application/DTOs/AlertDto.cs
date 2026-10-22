namespace JobRadar.Application.DTOs;

/// <summary>Representação pública de um alerta (nunca exponha a entidade de domínio diretamente).</summary>
public record AlertDto(
    Guid Id,
    Guid UserId,
    string? Technology,
    string? Location,
    string? Source,
    DateTime CreatedAt
);
