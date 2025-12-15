using System.Security.Claims;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventPostController : ControllerBase
{
    private readonly IEventPostService _eventPostService;
    private readonly IEventService _eventService;

    public EventPostController(IEventPostService eventPostService, IEventService eventService)
    {
        _eventPostService = eventPostService;
        _eventService = eventService;
    }

    /// <summary>
    /// Получить все посты на мероприятии
    /// </summary>
    [HttpGet("{eventId}/posts")]
    [Authorize]
    public async Task<IActionResult> GetPosts(Guid eventId, int count, int offset)
    {
        var result = await _eventPostService.GetPostsByEventIdAsync(eventId, count, offset);
        if (!result.Success)
            return NotFound(new { error = result.Error });
        
        return Ok(new { result = result.Data });
    }

    /// <summary>
    /// Получить пост мероприятия по ID
    /// </summary>
    [HttpGet("{eventId}/posts/{postId}")]
    [Authorize]
    public async Task<IActionResult> GetPost(Guid eventId, Guid postId)
    {
        var result = await _eventPostService.GetPostByIdAsync(eventId, postId);
        if (!result.Success)
            return NotFound(new { error = result.Error });

        return Ok(new { result = result.Data });
    }

    /// <summary>
    /// Создать пост на странице мероприятия
    /// </summary>
    [HttpPost("{eventId}/posts")]
    [Authorize]
    public async Task<IActionResult> CreatePost(Guid eventId, string text)
    {
        var authorId = GetUserIdFromToken();
        var result = await _eventPostService.AddPostAsync(eventId, authorId, text);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result = result.Data });
    }

    /// <summary>
    /// Обновить пост
    /// </summary>
    [HttpPut("{eventId}/posts/{postId}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(Guid postId, Guid eventId, string text)
    {
        var authorId = GetUserIdFromToken();
        var result = await _eventPostService.UpdatePostAsync(postId, eventId, authorId, text);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result = result.Data });
    }

    /// <summary>
    /// Удалить пост
    /// </summary>
    [HttpDelete("{eventId}/posts/{postId}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(Guid postId, Guid eventId)
    {
        var authorId = GetUserIdFromToken();
        var result = await _eventPostService.DeletePostAsync(postId, eventId, authorId);

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