using System.Security.Claims;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/invitations")]
public class InviteRespondController:ControllerBase
{
    private readonly IInviteService _inviteService;

    public InviteRespondController(IInviteService inviteService)
    {
        _inviteService = inviteService;
    }
    
    /// <summary>
    /// Ответить на приглашение
    /// </summary>
    [HttpPost("{invitationId}/respond")]
    [Authorize]
    public async Task<IActionResult> RespondToInvite(Guid invitationId, [FromQuery] bool accept)
    {
        var invite = await _inviteService.GetByIdAsync(invitationId);
        var result =
            await _inviteService.RespondToInviteAsync(invitationId, invite.Data.EventId, invite.Data.InvitedUserId, accept);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }
    
    /// <summary>
    /// Посмотреть присланные приглашения текущему пользователю
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetPendingInvites()
    {
        var userId = GetUserIdFromToken();
        var result = await _inviteService.GetPendingInvitesAsync(userId);
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