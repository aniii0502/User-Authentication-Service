using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Options;
using UserAuthService.Application.Services;   // TokenService namespace
using UserAuthService.Application.Config;     // JwtOptions namespace

namespace UserAuthService.Tests.Services
{
    public class TokenServiceTests
    {
        [Fact]
        public void TokenService_Should_Return_Correct_Secret_And_Expiry()
        {
            // Arrange
            var jwtOptions = Options.Create(new JwtOptions
            {
                Secret = "test-secret",
                AccessTokenExpiryMinutes = 30,
                RefreshTokenExpiryDays = 7,
                Issuer = "test",
                Audience = "test"
            });

            var tokenService = new TokenService(jwtOptions);

            // Act
            var secret = tokenService.GetSecret();
            var expiry = tokenService.GetExpiryMinutes();
            var refreshDays = tokenService.GetRefreshTokenDays();

            // Assert
            secret.Should().Be("test-secret");
            expiry.Should().Be(30);
            refreshDays.Should().Be(7);
        }
    }
}
