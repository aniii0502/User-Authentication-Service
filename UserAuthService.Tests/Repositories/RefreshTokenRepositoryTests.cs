using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UserAuthService.Domain.Entities;
using UserAuthService.Infrastructure;
using UserAuthService.Infrastructure.Repositories;
using UserAuthService.Infrastructure.Data;
using Xunit;

namespace UserAuthService.Tests.Repositories
{
    public class RefreshTokenRepositoryTests
    {
        private readonly RefreshTokenRepository _repo;
        private readonly AuthDbContext _context;
        private readonly User _user;

        public RefreshTokenRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AuthDbContext(options);
            _repo = new RefreshTokenRepository(_context);

            // Required user
            _user = new User { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", PasswordHash = "hashed" };
            _context.Users.Add(_user);
            _context.SaveChanges();
        }

        [Fact]
        public async Task AddAsync_Should_Add_RefreshToken()
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _user.Id,
                Token = "token-123",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _repo.AddAsync(token);

            var saved = await _context.RefreshTokens.FindAsync(token.Id);
            saved.Should().NotBeNull();
            saved.Token.Should().Be("token-123");
        }

        [Fact]
        public async Task UpdateAsync_Should_Update_RefreshToken()
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _user.Id,
                Token = "token-123",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _repo.AddAsync(token);

            token.Token = "updated-token";
            await _repo.UpdateAsync(token);

            var updated = await _context.RefreshTokens.FindAsync(token.Id);
            updated.Token.Should().Be("updated-token");
        }
    }
}
