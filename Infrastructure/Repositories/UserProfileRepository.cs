using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Domain.Interfaces;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly ApplicationDbContext _dbContext;
    
    public UserProfileRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(p => p.Id == id);
    }
    
    public async Task<User> CreateProfileAsync(Guid userId)
    {
        var profile = new User { Id = userId };
        
        await _dbContext.Users.AddAsync(profile);
        await _dbContext.SaveChangesAsync();
        
        return profile;
    }
    
    public async Task<User> UpdateProfileAsync(User userProfile)
    {
        _dbContext.Users.Update(userProfile);
        await _dbContext.SaveChangesAsync();
        
        return userProfile;
    }
    
    public async Task<bool> ProfileExistsAsync(Guid userId)
    {
        return await _dbContext.Users.AnyAsync(p => p.Id == userId);
    }
} 