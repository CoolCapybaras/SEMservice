using Dal.UserProfiles.Models;

namespace Dal.UserProfiles.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id);
    Task<UserProfile> CreateProfileAsync(Guid userId);
    Task<UserProfile> UpdateProfileAsync(UserProfile userProfile);
    Task<bool> ProfileExistsAsync(Guid userId);
} 