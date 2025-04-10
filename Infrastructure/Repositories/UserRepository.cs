using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Domain.Interfaces;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class UserRepository: IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
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
        if (string.IsNullOrEmpty(user.FirstName))
        {
            user.FirstName = "User"; // Присваиваем имя по умолчанию
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