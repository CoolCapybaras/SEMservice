using Domain;
using Domain.DTO;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IUserProfileService
{
    Task<ServiceResult<User>> GetProfileAsync(Guid userId);
    Task<ServiceResult<User>> UpdateProfileAsync(Guid userId, UpdateProfileRequest updateModel);
    Task<ServiceResult<String>> AddAvatarAsync(Guid userId, IFormFile? file);
    Task<ServiceResult<List<Event>>> GetSubscribedEventsAsync(Guid userId);
    Task<ServiceResult<List<User>>> GetOrganizersAsync();
} 