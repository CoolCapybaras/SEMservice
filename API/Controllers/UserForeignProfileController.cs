using System.Security.Claims;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.API.Controllers;
[ApiController]
[Route("api/users")]
public class UserForeignProfileController: ControllerBase
{
    private readonly IUserProfileService _profileService;

    public UserForeignProfileController(IUserProfileService profileService)
    {
        _profileService = profileService;
    }
    
    /// <summary>
    /// Получить профиль пользователя по ID
    /// </summary>
    [HttpGet("{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var result = await _profileService.GetProfileAsync(userId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(MapToResponse(result.Data!));
    }
    
    /// <summary>
    /// Получить все мероприятия на которые подписан пользователь
    /// </summary>
    [HttpGet("{userId}/events")]
    [Authorize]
    public async Task<IActionResult> GetSubscribedEvents(Guid userId)
    {
        var result = await _profileService.GetSubscribedEventsAsync(userId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data!);
    }
    
    /// <summary>
    /// Получить всех пользователей обладающих привелегией на создание мероприятий
    /// </summary>
    [HttpGet("organizers")]
    [Authorize]
    public async Task<IActionResult> GetOrganizers()
    {
        var result = await _profileService.GetOrganizersAsync();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data!);
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
            UserPrivilege = model.UserPrivilege.ToString(),
            AvatarUrl = model.AvatarUrl
        };
    }
    
}