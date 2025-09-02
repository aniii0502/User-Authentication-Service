using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using UserAuthService.Application.Services;
using UserAuthService.Application.Interfaces;
using UserAuthService.Domain.Entities;
using UserAuthService.Application.Config;
using System;
using System.Threading.Tasks;

namespace UserAuthService.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IRefreshTokenRepository> _refreshRepoMock;
        private readonly Mock<IPasswordResetRepository> _passwordResetRepoMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _refreshRepoMock = new Mock<IRefreshTokenRepository>();
            _passwordResetRepoMock = new Mock<IPasswordResetRepository>();
            _emailServiceMock = new Mock<IEmailService>();

            var jwtOptions = Options.Create(new JwtOptions
            {
                Secret = "supersecretkeysupersecretkey",
                Issuer = "test",
                Audience = "test",
                AccessTokenExpiryMinutes = 15,
                RefreshTokenExpiryDays = 7
            });

            _authService = new AuthService(
                _userRepoMock.Object,
                _refreshRepoMock.Object,
                _passwordResetRepoMock.Object,
                jwtOptions,
                _emailServiceMock.Object
            );
        }

        [Fact]
        public async Task RegisterAsync_Should_Create_User_When_Email_Not_Exists()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com"))
                         .ReturnsAsync((User)null);

            // Act
            var user = await _authService.RegisterAsync("John Doe", "test@example.com", "Password123!");

            // Assert
            user.Email.Should().Be("test@example.com");
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_Should_Throw_When_Email_Already_Exists()
        {
            _userRepoMock.Setup(r => r.GetByEmailAsync("exists@example.com"))
                         .ReturnsAsync(new User { Email = "exists@example.com" });

            Func<Task> act = async () => await _authService.RegisterAsync("Jane", "exists@example.com", "Password123!");

            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("Email already exists");
        }
    }
}
