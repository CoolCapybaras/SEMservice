using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IUserProfileRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User> CreateProfileAsync(Guid userId);
    Task<User> UpdateProfileAsync(User userProfile);
    Task<bool> ProfileExistsAsync(Guid userId);
    Task<String> AddAvatarAsync(Guid userId, string avatarUrl);
    Task<List<Event>> GetSubscribedEventsAsync(Guid userId);
    Task<List<User>> GetOrganizers();
} 