using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task AddUserAsync(User? user);
    
    Task<User> GetByResetTokenAsync(string token);

    Task UpdateUserAsync(User? user);
}