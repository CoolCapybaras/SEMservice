using System.Security.Claims;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventInviteController : ControllerBase
{
    private readonly IInviteService _inviteService;

    public EventInviteController(IInviteService inviteService)
    {
        _inviteService = inviteService;
    }

    /// <summary>
    /// Пригласить пользователя на мероприятие
    /// </summary>
    [HttpPost("{eventId}/invitations")]
    [Authorize]
    public async Task<IActionResult> SendInvite(Guid eventId, Guid invitedId)
    {
        var inviter = GetUserIdFromToken();
        var result = await _inviteService.SendInviteAsync(eventId, invitedId, inviter);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result = result.Data });
    }
    
    /// <summary>
    /// Получить приглашённого пользователя на мероприятие
    /// </summary>
    [HttpGet("{eventId}/invitations/{invitationId}")]
    [Authorize]
    public async Task<IActionResult> GetInvitedUser(Guid invitationId, Guid eventId)
    {
        var result = await _inviteService.GetInvitedUserAsync(invitationId, eventId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result = result.Data });
    }
    
    /// <summary>
    /// Получить список приглашённых пользователей на мероприятие
    /// </summary>
    [HttpGet("{eventId}/invitations")]
    [Authorize]
    public async Task<IActionResult> GetInvitedUsers(Guid eventId, int count, int offset)
    {
        var result = await _inviteService.GetInvitedUsersAsync(eventId, count,offset);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result = result.Data });
    }
    
    /// <summary>
    /// Отозвать приглашение на мероприятие
    /// </summary>
    [HttpDelete("{eventId}/invitations/{invitationId}")]
    [Authorize]
    public async Task<IActionResult> DeleteInvitation(Guid invitationId, Guid eventId)
    {
        var currentUserId = GetUserIdFromToken();
        var result = await _inviteService.DeleteInviteAsync(invitationId, eventId, currentUserId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result = result.Data });
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