using System.Security.Claims;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/attachments")]
public class EventAttachmentsController : ControllerBase
{
    private readonly IEventAttachmentService _attachmentService;

    public EventAttachmentsController(IEventAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }
    
    /// <summary>
    /// Получить все вложения
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(Guid eventId, [FromQuery] EventAttachmentListQuery? query)
    {
        var userId = GetUserIdFromToken();
        var result = await _attachmentService.GetByEventAsync(eventId, userId, query);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Динамические значения для фильтра: расширения файлов в этом мероприятии, площадки ссылок, авторы.
    /// </summary>
    [HttpGet("facets")]
    [Authorize]
    public async Task<IActionResult> GetFacets(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        var result = await _attachmentService.GetFacetsAsync(eventId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Загрузить вложение
    /// </summary>
    [HttpPost("file")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> UploadFile(Guid eventId, IFormFile file, [FromForm] string? title)
    {
        var userId = GetUserIdFromToken();
        var result = await _attachmentService.UploadFileAsync(eventId, userId, file, title);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Загрузить ссылку
    /// </summary>
    [HttpPost("link")]
    [Authorize]
    public async Task<IActionResult> AddLink(Guid eventId, [FromBody] EventAttachmentLinkRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _attachmentService.AddLinkAsync(eventId, userId, request);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Скачать вложение
    /// </summary>
    [HttpGet("{attachmentId:guid}/download")]
    [Authorize]
    public async Task<IActionResult> Download(Guid eventId, Guid attachmentId)
    {
        var userId = GetUserIdFromToken();
        var result = await _attachmentService.GetByIdAsync(eventId, attachmentId, userId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        var attachment = result.Data!;
        if (attachment.Kind == EventAttachmentKind.Link)
            return Ok(new { result = attachment });

        var relative = attachment.Resource.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
        if (!System.IO.File.Exists(fullPath))
            return NotFound(new { error = "Файл не найден" });

        return PhysicalFile(fullPath, attachment.ContentType ?? "application/octet-stream", attachment.OriginalFileName ?? attachment.Title);
    }

    /// <summary>
    /// Удалить вложение
    /// </summary>
    [HttpDelete("{attachmentId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid eventId, Guid attachmentId)
    {
        var userId = GetUserIdFromToken();
        var result = await _attachmentService.DeleteAsync(eventId, attachmentId, userId);
        return result.Success ? Ok(new { result = true }) : BadRequest(new { error = result.Error });
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new Exception("Некорректный идентификатор пользователя в токене");
        return userId;
    }
}
