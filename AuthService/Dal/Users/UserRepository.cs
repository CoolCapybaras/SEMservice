using Dal.Users.Interfaces;
using Dal.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.Users;

public class UserRepository: IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User> GetByResetTokenAsync(string token)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == token);
    }

    public async Task AddUserAsync(User user)
    {
        if (string.IsNullOrEmpty(user.Username))
        {
            user.Username = "User"; // Присваиваем имя по умолчанию
        }
            
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}