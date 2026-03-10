using System.Security.Claims;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/chat")]
public class EventChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public EventChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Получить сообщения чата мероприятия
    /// </summary>
    [HttpGet("messages")]
    [Authorize]
    public async Task<IActionResult> GetMessages(Guid eventId, int count, int offset)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.GetMessagesAsync(eventId, count, offset, userId);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
    }

    public class SendMessageRequest
    {
        public string Text { get; set; }
    }

    /// <summary>
    /// Отправить сообщение в чат мероприятия
    /// </summary>
    [HttpPost("messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(Guid eventId, [FromBody] SendMessageRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.AddMessageAsync(eventId, userId, request.Text);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
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

