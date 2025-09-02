using System.Threading.Tasks;
using UserAuthService.Domain.Entities;

namespace UserAuthService.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);
        //Task CreateAsync(User user);
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task UpdateAsync(RefreshToken token);
        Task DeleteAsync(RefreshToken token);
    }
}
