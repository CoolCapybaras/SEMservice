using System.Security.Claims;
using Domain.DTO;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/event")]
public class EventController: ControllerBase
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService)
    {
        _eventService = eventService;
    }
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        var createdEvent = await _eventService.CreateEventAsync(request);
            
        return Ok(createdEvent);
    }

    [HttpGet("{eventId}/event")]
    public async Task<IActionResult> GetEventById(Guid eventId)
    {
        var @event = await _eventService.GetEventAsync(eventId);

        return Ok(@event);
    }

    [HttpPost("events")]
    public async Task<IActionResult> SearchEvents([FromBody] SearchRequest request, int offset, int count)
    {
        var events = await _eventService.SerchEventsAsync(request, offset, count);
        return Ok(events);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _eventService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteEvent(Guid eventId)
    {
        await _eventService.DeletEvent(eventId);
        return Ok("Ивент удалён");
    }
}