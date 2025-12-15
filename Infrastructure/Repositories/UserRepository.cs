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

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _context.RefreshTokens.AddAsync(token, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
    }

    public async Task RevokeAsync(RefreshToken token, CancellationToken ct = default)
    {
        token.IsRevoked = true;
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync(ct);
        foreach (var t in tokens) t.IsRevoked = true;
        _context.RefreshTokens.UpdateRange(tokens);
        await _context.SaveChangesAsync(ct);
    }
}