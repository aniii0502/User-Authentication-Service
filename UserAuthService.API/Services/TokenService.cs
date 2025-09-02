using Microsoft.Extensions.Options;
using UserAuthService.API.Config;

namespace UserAuthService.API.Services
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
    }
}

