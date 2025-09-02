using System;
using System.Threading.Tasks;
using UserAuthService.Domain.Entities;

namespace UserAuthService.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByUsernameAsync(string username);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        //Task CreateAsync(User user);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
    }
}
