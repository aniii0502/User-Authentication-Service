using Microsoft.EntityFrameworkCore;
using UserAuthService.Domain.Entities;
using UserAuthService.Application.Interfaces;

namespace UserAuthService.Infrastructure.Data
{
    public class AuthDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        // Add this constructor for design-time support
        public AuthDbContext() { }
    }
}
