using Dal.UserProfiles.Interfaces;
using Dal.UserProfiles.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.UserProfiles;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly UserDbContext _dbContext;
    
    public UserProfileRepository(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<UserProfile?> GetByIdAsync(Guid id)
    {
        return await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.Id == id);
    }
    
    public async Task<UserProfile> CreateProfileAsync(Guid userId)
    {
        var profile = new UserProfile { Id = userId };
        
        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.SaveChangesAsync();
        
        return profile;
    }
    
    public async Task<UserProfile> UpdateProfileAsync(UserProfile userProfile)
    {
        _dbContext.UserProfiles.Update(userProfile);
        await _dbContext.SaveChangesAsync();
        
        return userProfile;
    }
    
    public async Task<bool> ProfileExistsAsync(Guid userId)
    {
        return await _dbContext.UserProfiles.AnyAsync(p => p.Id == userId);
    }
} 