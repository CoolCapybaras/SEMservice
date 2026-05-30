using Domain.DTO;
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

    public async Task<String> AddAvatarAsync(Guid userId, string avatarUrl)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(p => p.Id == userId);
        user.AvatarUrl = avatarUrl;
        await _dbContext.SaveChangesAsync();
        
        return user.AvatarUrl;
    }

    public async Task<List<Event>> GetSubscribedEventsAsync(Guid userId)
    {
        return await _dbContext.Events
            .Where(e => e.EventRoles.Any(r => r.UserId == userId))
            .Include(e => e.EventCategories)
            .ThenInclude(ec => ec.Category)
            .Include(e => e.SelectedTypes)
            .Include(e => e.EventRoles)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }
    
    public async Task<List<User>> GetOrganizers()
    {
        return await _dbContext.Users
            .Where(e => e.UserPrivilege == UserPrivilege.ORGANIZER || e.UserPrivilege == UserPrivilege.ADMIN)
            .ToListAsync();
    }

    public async Task<List<UserProfileResponse>> GetUsersListAsync(UserListRequest request)
    {
        var query = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var pattern = $"%{request.Q.Trim()}%";
            query = query.Where(u =>
                (u.FirstName != null && EF.Functions.ILike(u.FirstName, pattern)) ||
                (u.LastName != null && EF.Functions.ILike(u.LastName, pattern)) ||
                (u.Profession != null && EF.Functions.ILike(u.Profession, pattern)) ||
                (u.City != null && EF.Functions.ILike(u.City, pattern)));
        }

        var count = request.Count <= 0 ? 20 : Math.Min(request.Count, 100);

        return await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(request.Offset)
            .Take(count)
            .Select(u => new UserProfileResponse
            {
                Id = u.Id,
                LastName = u.LastName,
                FirstName = u.FirstName,
                Profession = u.Profession,
                City = u.City,
                AvatarUrl = u.AvatarUrl,
                UserPrivilege = u.UserPrivilege.ToString()
            })
            .ToListAsync();
    }

    public async Task<SystemRoleResponse> GetSystemRoleAsync(Guid userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(p => p.Id == userId);
        var systemRoleResponse = new SystemRoleResponse
        {
            RoleName = user.UserPrivilege.ToString(),
        };
        return systemRoleResponse;
    }

    public async Task<bool> ProfileExistsAsync(Guid userId)
    {
        return await _dbContext.Users.AnyAsync(p => p.Id == userId);
    }
} 