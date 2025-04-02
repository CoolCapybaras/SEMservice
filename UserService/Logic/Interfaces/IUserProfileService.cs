using Logic.Models;

namespace Logic.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileModel?> GetProfileAsync(Guid userId);
    Task<UserProfileModel> UpdateProfileAsync(Guid userId, UpdateProfileModel updateModel);
    Task<UserProfileModel> CreateProfileIfNotExistsAsync(Guid userId);
} 