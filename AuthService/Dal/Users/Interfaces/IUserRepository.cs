using Dal.Users.Models;

namespace Dal.Users.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task AddUserAsync(User? user);
    
    Task<User> GetByResetTokenAsync(string token);

    Task UpdateUserAsync(User? user);
}