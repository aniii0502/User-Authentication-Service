using Microsoft.Extensions.Options;

using UserAuthService.Application.Config;
namespace UserAuthService.Application.Services
{
    public class TokenService
    {
        private readonly JwtOptions _jwtOptions;

        public TokenService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public string GetSecret() => _jwtOptions.Secret;

        public int GetExpiryMinutes() => _jwtOptions.AccessTokenExpiryMinutes;
        public int GetRefreshTokenDays() => _jwtOptions.RefreshTokenExpiryDays;

    }
}

