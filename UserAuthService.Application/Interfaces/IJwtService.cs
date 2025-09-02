using UserAuthService.Domain.Entities;

namespace UserAuthService.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
    }
}