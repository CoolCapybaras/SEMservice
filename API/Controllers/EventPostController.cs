using System.Security.Claims;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events/posts")]
public class EventPostController : ControllerBase
{
    private readonly IEventPostService _eventPostService;
    private readonly IEventService _eventService;

    public EventPostController(IEventPostService eventPostService, IEventService eventService)
    {
        _eventPostService = eventPostService;
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts(Guid eventId)
    {
        var posts = await _eventPostService.GetPostsByEventIdAsync(eventId);
        return Ok(new {result = posts});
    }

    [HttpGet("{postId}")]
    public async Task<IActionResult> GetPost(Guid postId)
    {
        var post = await _eventPostService.GetPostByIdAsync(postId);
        if (post == null)
            return NotFound();

        return Ok(new {result = post});
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost(Guid eventId, string text)
    {
        var authorId = GetUserIdFromToken();
        
        var @event = await _eventService.GetEventByIdAsync(eventId);
        if (authorId == @event.ResponsiblePersonId)
        {
            var eventPost = new EventPost
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                AuthorId = authorId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
            };

            await _eventPostService.AddPostAsync(eventPost);
            return Ok(new {result = eventPost});
        }
        else
        {
            return Unauthorized("Вы не являетесь владельцем мероприятия");
        }
    }

    [HttpPut("{postId}")]
    public async Task<IActionResult> UpdatePost(Guid postId, Guid eventId, string text)
    {
        var authorId = GetUserIdFromToken();
        var post = await _eventPostService.GetPostByIdAsync(postId);
        var @event = await _eventService.GetEventByIdAsync(eventId);
        if (authorId == @event.ResponsiblePersonId)
        {
            post.Text = text;
            await _eventPostService.UpdatePostAsync(post);
            return Ok(new {result = post});
        }
        else
        {
            return Unauthorized("Вы не являетесь владельцем мероприятия");
        }
    }

    [HttpDelete("{postId}")]
    public async Task<IActionResult> DeletePost(Guid postId, Guid eventId)
    {
        var authorId = GetUserIdFromToken();
        
        var @event = await _eventService.GetEventByIdAsync(eventId);
        if (authorId == @event.ResponsiblePersonId)
        {
            await _eventPostService.DeletePostAsync(postId);
            return NoContent();
        }
        else
        {
            return Unauthorized("Вы не являетесь владельцем мероприятия");
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
}