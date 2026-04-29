using SEM.Domain.Models;

namespace Domain.DTO;

public class EventResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Location { get; set; } = null!;

    public string? Auditorium { get; set; }

    public VenueFormat VenueFormat { get; set; }

    public List<EventTypeKind> Types { get; set; } = new();

    public Guid ResponsiblePersonId { get; set; }

    public int? MaxParticipants { get; set; }

    public string Color { get; set; } = null!;

    public List<string> Categories { get; set; } = new();

    public List<string> PreviewPhotos { get; set; } = new();

    public EventLifecycleState LifecycleState { get; set; }
    
    public bool IsCancelled { get; set; }
    
    public int BufferDays { get; set; }

    public List<UserResponse> Participants { get; set; } = new();

    public int ParticipantsCount { get; set; }

    public string Avatar { get; set; } = null!;
}