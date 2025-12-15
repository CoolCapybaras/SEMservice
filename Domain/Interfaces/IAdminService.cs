using Domain.DTO;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IAdminService
{
    Task<ServiceResult<List<UserProfileResponse>>> GetUserListAsync(UserSerchRequest request, Guid userId);
    Task<ServiceResult<User>> UpdateProfileAsync(Guid userId, UpdateProfileRequest updateModel, IFormFile? file, Guid adminId);
    Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid adminId);
    Task<ServiceResult<bool>> GivePrivelegeToUserAsync(Guid userId, Guid adminId);
}