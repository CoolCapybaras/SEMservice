using System.Security.Claims;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/notes")]
public class EventNotesController : ControllerBase
{
    private readonly IEventNoteService _noteService;

    public EventNotesController(IEventNoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        var result = await _noteService.GetByEventAsync(eventId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Guid eventId, [FromBody] EventNoteRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _noteService.CreateAsync(eventId, userId, request.Text);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{noteId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid eventId, Guid noteId, [FromBody] EventNoteRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _noteService.UpdateAsync(eventId, noteId, userId, request.Text);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    [HttpDelete("{noteId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid eventId, Guid noteId)
    {
        var userId = GetUserIdFromToken();
        var result = await _noteService.DeleteAsync(eventId, noteId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new Exception("Некорректный идентификатор пользователя в токене");
        return userId;
    }
}