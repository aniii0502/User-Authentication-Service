using System.Threading.Tasks;
using UserAuthService.Domain.Entities;

public interface IPasswordResetRepository
{
    Task AddAsync(PasswordResetToken token);
    //Task CreateAsync(User user);
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task UpdateAsync(PasswordResetToken token);
}
