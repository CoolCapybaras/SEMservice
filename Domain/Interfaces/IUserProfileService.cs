using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IUserProfileService
{
    Task<User?> GetProfileAsync(Guid userId);
    Task<User> UpdateProfileAsync(Guid userId, User updateModel);
    Task<User> CreateProfileIfNotExistsAsync(Guid userId);
} 