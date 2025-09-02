using Xunit;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using UserAuthService.Infrastructure.Repositories;
using UserAuthService.Domain.Entities;
using UserAuthService.Infrastructure;

namespace UserAuthService.Tests.Repositories
{
    public class PasswordResetRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Should_Add_PasswordResetToken()
        {
            using var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var repo = new PasswordResetRepository(context);

            var user = new User { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", PasswordHash = "hashed" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var resetToken = new PasswordResetToken
            {
                TokenHash = "reset-123",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            await repo.AddAsync(resetToken);

            var result = await repo.GetByTokenAsync("reset-123");
            result.Should().NotBeNull();
            result!.TokenHash.Should().Be("reset-123");
        }

        [Fact]
        public async Task UpdateAsync_Should_Update_Token_IsUsed()
        {
            using var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var repo = new PasswordResetRepository(context);

            var user = new User { Id = Guid.NewGuid(), FullName = "Jane Doe", Email = "jane@example.com", PasswordHash = "hashed" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var resetToken = new PasswordResetToken
            {
                TokenHash = "reset-xyz",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            await repo.AddAsync(resetToken);

            resetToken.IsUsed = true;
            await repo.UpdateAsync(resetToken);

            var result = await repo.GetByTokenAsync("reset-xyz");
            result.Should().BeNull(); // filtered by !IsUsed
        }

        [Fact]
        public async Task DeleteByUserIdAsync_Should_Remove_All_UserTokens()
        {
            using var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var repo = new PasswordResetRepository(context);

            var user = new User { Id = Guid.NewGuid(), FullName = "Bob Smith", Email = "bob@example.com", PasswordHash = "hashed" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var resetToken1 = new PasswordResetToken
            {
                TokenHash = "reset-1",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            var resetToken2 = new PasswordResetToken
            {
                TokenHash = "reset-2",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            await repo.AddAsync(resetToken1);
            await repo.AddAsync(resetToken2);

            await repo.DeleteByUserIdAsync(user.Id);

            (await repo.GetByTokenAsync("reset-1")).Should().BeNull();
            (await repo.GetByTokenAsync("reset-2")).Should().BeNull();
        }
    }
}
