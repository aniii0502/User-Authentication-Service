using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using UserAuthService.Application.Interfaces;
using UserAuthService.Domain.Entities;
using UserAuthService.Infrastructure.Data;

namespace UserAuthService.Infrastructure.Repositories
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly AuthDbContext _context;

        public PasswordResetRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<PasswordResetToken?> GetByTokenAsync(string token)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == token && !t.IsUsed);
        }

        public async Task UpdateAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            var tokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
            _context.PasswordResetTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }
    }
}
