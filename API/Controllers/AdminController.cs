using System.Security.Claims;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminController: ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }
    /// <summary>
    /// Получить список всех пользователей
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUsersList([FromQuery] UserSerchRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _adminService.GetUserListAsync(request, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Обновить профиль определённого пользователя
    /// </summary>
    [HttpPut("{userId}")]
    [Authorize]
    public async Task<IActionResult> UpdateUserProfile(Guid userId, IFormFile? file,[FromForm] UpdateProfileRequest request)
    {
        var adminId = GetUserIdFromToken();
        var result = await _adminService.UpdateProfileAsync(userId, request, file, adminId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(MapToResponse(result.Data!));
    }
    
    /// <summary>
    /// Дать пользователю право создавать мероприятия
    /// </summary>
    [HttpPost("{userId}")]
    [Authorize]
    public async Task<IActionResult> GivePrivelegeToUser(Guid userId)
    {
        var adminId = GetUserIdFromToken();
        var result = await _adminService.GivePrivelegeToUserAsync(userId, adminId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok();
    }

    
    /// <summary>
    /// Удалить пользователя
    /// </summary>
    [HttpDelete("{userId}")]
    [Authorize]
    public async Task<IActionResult> DeleteSuscriber(Guid userId)
    {
        var adminId = GetUserIdFromToken();
        var result = await _adminService.DeleteUserAsync(userId, adminId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
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
            City = model.City,
            UserPrivilege = model.UserPrivilege.ToString(),
            AvatarUrl = model.AvatarUrl
        };
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
}