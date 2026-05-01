using JobRadar.Domain.Entities;

namespace JobRadar.Application.Abstractions;

public interface ITokenService
{
    string GenerateToken(User user);
}