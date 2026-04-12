using SEM.Domain.Models;

namespace Domain.DTO;

public class EventUpdateRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Location { get; set; }

    public string? Auditorium { get; set; }

    public VenueFormat? VenueFormat { get; set; }

    public List<EventTypeKind>? Types { get; set; }

    public int? MaxParticipants { get; set; }

    public string? Color { get; set; }
}