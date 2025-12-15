using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IAdminRepository
{
    Task<List<UserProfileResponse>> GetUserListAsync(UserSerchRequest request);
    Task<User> UpdateProfileAsync(User userProfile);
    Task DeleteUserAsync(Guid userId);
    Task<bool> UserIsAdmin(Guid userId);
    Task GivePrivelegeToUserAsync(Guid userId);
}