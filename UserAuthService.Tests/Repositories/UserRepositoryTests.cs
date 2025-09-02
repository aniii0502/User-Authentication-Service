using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UserAuthService.Infrastructure;
using UserAuthService.Infrastructure.Repositories;
using UserAuthService.Infrastructure.Data;
using UserAuthService.Domain.Entities;
using Xunit;

namespace UserAuthService.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private readonly UserRepository _userRepository;
        private readonly AuthDbContext _context;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AuthDbContext(options);
            _userRepository = new UserRepository(_context);
        }

        [Fact]
        public async Task AddAsync_Should_Add_User_To_Database()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                Email = "john@example.com",
                PasswordHash = "hashed-password"
            };

            await _userRepository.AddAsync(user);

            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser.FullName.Should().Be("John Doe");
        }
    }
}
