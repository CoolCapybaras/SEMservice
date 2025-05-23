using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SEM.Domain.Models;
using SEM.Domain.Interfaces;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserProfileService profileService, ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var userId = GetUserIdFromToken();
            
            // Создаем профиль, если он не существует
            await _profileService.CreateProfileIfNotExistsAsync(userId);
            
            // Получаем профиль
            var profile = await _profileService.GetProfileAsync(userId);
            
            if (profile == null)
            {
                return NotFound(new { message = "Профиль не найден" });
            }
            
            var response = MapToResponse(profile);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении профиля пользователя");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUserProfile(IFormFile? file,[FromForm] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            
            var updateModel = new User
            {
                LastName = request.LastName,
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                PhoneNumber = request.PhoneNumber,
                Telegram = request.Telegram,
                City = request.City,
                EducationalInstitution = request.EducationalInstitution,
                CourseNumber = request.CourseNumber
            };
            
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "avatars");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var avatarUrl = $"/avatars/{fileName}";
                updateModel.AvatarUrl = avatarUrl;
            }
            
            var updatedProfile = await _profileService.UpdateProfileAsync(userId, updateModel);
            
            var response = MapToResponse(updatedProfile);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении профиля пользователя");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
    
    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new Exception("Некорректный идентификатор пользователя в токене");
        }
        
        return userId;
    }
    
    private static UserProfileResponse MapToResponse(User model)
    {
        return new UserProfileResponse
        {
            Id = model.Id,
            LastName = model.LastName,
            FirstName = model.FirstName,
            MiddleName = model.MiddleName,
            PhoneNumber = model.PhoneNumber,
            Telegram = model.Telegram,
            City = model.City,
            EducationalInstitution = model.EducationalInstitution,
            CourseNumber = model.CourseNumber,
            AvatarUrl = model.AvatarUrl
        };
    }
    
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Telegram { get; set; }
        public string? City { get; set; }
        public string? EducationalInstitution { get; set; }
        public int? CourseNumber { get; set; }
        
        public string? AvatarUrl { get; set; }
    } 
    
    public class UpdateProfileRequest
    {
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Telegram { get; set; }
        public string? City { get; set; }
        public string? EducationalInstitution { get; set; }
        public int? CourseNumber { get; set; }
    } 
} 