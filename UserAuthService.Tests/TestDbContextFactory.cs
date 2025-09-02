using Microsoft.EntityFrameworkCore;
using UserAuthService.Infrastructure.Data;

namespace UserAuthService.Tests
{
    public static class TestDbContextFactory
    {
        public static AuthDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AuthDbContext(options);
        }
    }
}
