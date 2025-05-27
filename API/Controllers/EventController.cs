using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain.DTO;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        var createdEvent = await _eventService.CreateEventAsync(request);
        return Ok(new {result = createdEvent});
    }

    [HttpGet("{eventId}")]
    public async Task<IActionResult> GetEventById(Guid eventId)
    {
        var _event = await _eventService.GetEventByIdAsync(eventId);
        return Ok(new {result=_event});
    }

    [HttpGet]
    public async Task<IActionResult> SearchEvents([FromQuery] SearchRequest request)
    {
        var events = await _eventService.SearchEventsAsync(request);
        return Ok(new {result=events});
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteEvent(Guid eventId)
    {
        await _eventService.DeleteEventAsync(eventId);
        return Ok(new {reuslt = eventId});
    }

    [HttpPost("{eventId}")]
    public async Task<IActionResult> AddSuscriberAsync(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        await _eventService.AddSuscriberAsync(eventId, userId);
        return Ok(new{result = eventId, userId});
    }

    [HttpDelete("{eventId}")]
    public async Task<IActionResult> DeleteSuscriber(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        await _eventService.DeleteSuscriber(eventId, userId);
        return Ok(new {result = eventId, userId});
    }

    [HttpPost("{eventId}/{userId}")]
    public async Task<IActionResult> AddRoleToUser(Guid eventId, Guid userId,string roleName)
    {
        var сurentUserId = GetUserIdFromToken();
        var @event = await _eventService.GetEventByIdAsync(eventId);
        var role = await _eventService.GetRoleByName(roleName);
        if (role == null)
            return BadRequest("Роль не найдена");
        if (сurentUserId == @event.ResponsiblePersonId)
        {
            await _eventService.AddRoleToUser(eventId,  userId, roleName);
            return Ok(new {result = eventId, roleName, userId});
        }
        else
        {
            return Unauthorized("Вы не являетесь создателем мероприятия");
        }
    }

    [HttpGet("roles/{eventId}")]
    public async Task<IActionResult> GetRolesByEvent(Guid eventId)
    {
        var result = await _eventService.GetRolesByEvent(eventId);
        return Ok(new { res = result });
    }

    [HttpGet("subscribers")]
    public async Task<IActionResult> GetAllSuscribersAsync(Guid eventId)
    {
        var result = await _eventService.GetAllSuscribersAsync(eventId);
        return Ok(new { res = result });
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateEvent(Guid eventId, [FromBody] EventUpdateRequest request)
    {
        var userId = GetUserIdFromToken();
        var @event = await _eventService.GetEventByIdAsync(eventId);
        if (userId == @event.ResponsiblePersonId)
        {
            var updateModel = new Event
            {
                Name = request.Name,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Location = request.Location,
                Format = request.Format,
                EventType = request.EventType,
                MaxParticipants = request.MaxParticipants
            };

            var updatedEvent = await _eventService.UpdateEventAsync(eventId, updateModel);

            return Ok(new {result = updatedEvent});
        }
        else
        {
            return Unauthorized("Вы не являетесь создателем мероприятия");
        }
    }
    
    [HttpGet("{eventId}/photos")]
    public async Task<IActionResult> GetEventPhotos(Guid eventId)
    {
        var photos = await _eventService.GetEventPhotosAsync(eventId);
        return Ok(new {result = photos});
    }
    
    [HttpPost("{eventId}/photos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadEventPhoto(Guid eventId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не загружен.");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = Path.Combine("uploads", fileName).Replace("\\", "/");
        await _eventService.AddEventPhotoAsync(eventId, "/" + relativePath);

        return Ok(new { path = "/" + relativePath });
    }

    [HttpPost("contact")]
    public async Task<IActionResult> AddContact(Guid eventId, Guid userId)
    {
        await _eventService.AddContact(eventId, userId);
        return Ok(new {result = eventId, userId});
    }

    [HttpGet("{eventId}/contacts")]
    public async Task<IActionResult> GetContacts(Guid eventId)
    {
        var contacts = await _eventService.GetContacts(eventId);

        return Ok(new {result = contacts});
        
    }

    [HttpGet("users")]
    public async Task<IActionResult> Get10Users(string userName)
    {
        var users = await _eventService.Get10UsersByName(userName);

        var res = new List<UserResponse>();

        foreach (var user in users)
        {
            var userResp = new UserResponse
            {
                Id = user.Id,
                LastName = user.LastName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                AvatarUrl = user.AvatarUrl
            };
            
            res.Add(userResp);
        }

        return Ok(new { result = res });
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

    public class EventUpdateRequest
    {

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Location { get; set; }

        public string Format { get; set; }

        public string EventType { get; set; }

        public int? MaxParticipants { get; set; }
    }

}