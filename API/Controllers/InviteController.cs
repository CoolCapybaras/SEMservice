using System.Security.Claims;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/invites")]
public class InviteController : ControllerBase
{
    private readonly IInviteService _inviteService;

    public InviteController(IInviteService inviteService)
    {
        _inviteService = inviteService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendInvite(Guid eventId, Guid invitedId)
    {
        var inviter = GetUserIdFromToken();
        var invite = await _inviteService.SendInviteAsync(eventId, invitedId, inviter);
        return Ok(new {result = invite});
    }

    [HttpPost("{inviteId}/respond")]
    public async Task<IActionResult> RespondToInvite(Guid inviteId, [FromQuery] bool accept)
    {
        var invite = await _inviteService.GetByIdAsync(inviteId);
        await _inviteService.RespondToInviteAsync(inviteId, invite.EventId, invite.InvitedUserId, accept);
        return Ok(new {result = invite});
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingInvites()
    {
        var userId = GetUserIdFromToken();
        var invites = await _inviteService.GetPendingInvitesAsync(userId);
        return Ok(new {result = invites});
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