using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventController: ControllerBase
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService)
    {
        _eventService = eventService;
    }
    
    [HttpPost("event_create")]
    public async Task<IActionResult> Register([FromBody] NewEventRequest request)
    {
        var newEvent = new Event
        {
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            Format = request.Format,
            EventType = request.EventType,
            ResponsiblePerson = request.ResponsiblePerson,
            MaxParticipants = request.MaxParticipants
        };

        var createdEvent = await _eventService.CreateEventAsync(newEvent);
        
        var response = MapToResponse(createdEvent);
            
        return Ok(response);
    }

    private static Event MapToResponse(Event nEvent)
    {
        return new Event
        {
            Name = nEvent.Name,
            Description = nEvent.Description,
            StartDate = nEvent.StartDate,
            EndDate = nEvent.EndDate,
            Location = nEvent.Location,
            Format = nEvent.Format,
            EventType = nEvent.EventType,
            ResponsiblePerson = nEvent.ResponsiblePerson,
            MaxParticipants = nEvent.MaxParticipants
        };
    }

    public class NewEventRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Location { get; set; }

        public string Format { get; set; }

        public string EventType { get; set; }

        public string ResponsiblePerson { get; set; }

        public int? MaxParticipants { get; set; }
    }
}