using Domain.DTO;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;

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
        return Ok(createdEvent);
    }

    [HttpGet("{eventId}")]
    public async Task<IActionResult> GetEventById(Guid eventId)
    {
        var _event = await _eventService.GetEventByIdAsync(eventId);
        return Ok(_event);
    }

    [HttpGet]
    public async Task<IActionResult> SearchEvents([FromQuery] SearchRequest request)
    {
        var events = await _eventService.SearchEventsAsync(request);
        return Ok(events);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteEvent(Guid eventId)
    {
        await _eventService.DeleteEventAsync(eventId);
        return Ok("Ивент удалён");
    }
}