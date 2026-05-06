using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SEM.Domain.Models;
using SEM.Domain.Interfaces;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/users/me")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly IBoardTaskService _boardTaskService;

    public ProfileController(IUserProfileService profileService, IBoardTaskService boardTaskService)
    {
        _profileService = profileService;
        _boardTaskService = boardTaskService;
    }
    
    /// <summary>
    /// Получить профиль текущего пользователя
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = GetUserIdFromToken();

        var result = await _profileService.GetProfileAsync(userId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(MapToResponse(result.Data!));
    }
    
    /// <summary>
    /// Обновить профиль текущего пользователя
    /// </summary>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _profileService.UpdateProfileAsync(userId, request);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(MapToResponse(result.Data!));
    }
    
    /// <summary>
    /// Обновить аватар текущего пользователя
    /// </summary>
    [HttpPost("avatar")]
    [Authorize]
    public async Task<IActionResult> AddAvatar(IFormFile? file)
    {
        var userId = GetUserIdFromToken();
        var result = await _profileService.AddAvatarAsync(userId, file);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data!);
    }
    
    /// <summary>
    /// Получить все мероприятия на которые подписан текущий пользователь
    /// </summary>
    [HttpGet("events")]
    [Authorize]
    public async Task<IActionResult> GetSubscribedEvents()
    {
        var userId = GetUserIdFromToken();
        var result = await _profileService.GetSubscribedEventsAsync(userId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data!);
    }
    
    /// <summary>
    /// Получить мои назначенные задачи (по всем мероприятиям)
    /// </summary>
    [HttpGet("board/my-tasks")]
    [Authorize]
    public async Task<IActionResult> GetMyBoardTasks()
    {
        var userId = GetUserIdFromToken();
        var result = await _boardTaskService.GetCurrentUserTasksAsync(userId);
        return Ok(result.Data);
    }

    [HttpGet("system/role")]
    [Authorize]
    public async Task<IActionResult> GetSystemRole()
    {
        var userId = GetUserIdFromToken();
        var result = await _profileService.GetSystemRoleAsync(userId);
        
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data!);
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
            Profession = model.Profession,
            PhoneNumber = model.PhoneNumber,
            Telegram = model.Telegram,
            Vk = model.Vk,
            City = model.City,
            UserPrivilege = model.UserPrivilege.ToString(),
            AvatarUrl = model.AvatarUrl,
            Theme = model.Theme.ToString(),
            NotificationChannel = model.NotificationChannel.ToString(),
            NotifyTaskAssigned = model.NotifyTaskAssigned,
            NotifyTaskDeadline = model.NotifyTaskDeadline,
            NotifyEventStart = model.NotifyEventStart,
            NotifyEventCancelled = model.NotifyEventCancelled
        };
    }
} 