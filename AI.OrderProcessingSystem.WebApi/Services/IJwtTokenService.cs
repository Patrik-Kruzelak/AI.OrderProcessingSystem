using AI.OrderProcessingSystem.Dal.Entities;

namespace AI.OrderProcessingSystem.WebApi.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    DateTime GetTokenExpiration();
}
