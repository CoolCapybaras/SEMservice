using SEM.Domain.Models;

namespace Domain.DTO;

public class EventRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Location { get; set; } = null!;

    public string? Auditorium { get; set; }

    public VenueFormat VenueFormat { get; set; }

    /// <summary>Теги (как прежние категории): произвольные строки, many-to-many.</summary>
    public List<string> Categories { get; set; } = new();

    public List<EventTypeKind> Types { get; set; } = new();

    public List<EventParticipantAssignmentDto> Participants { get; set; } = new();

    public Guid ResponsiblePersonId { get; set; }

    public int? MaxParticipants { get; set; }

    public string Color { get; set; } = null!;
    
    public int? BufferDays { get; set; }

    /// <summary>
    /// Если true — создаём мероприятие сразу в Published. Если false — создаём как черновик (Draft).
    /// </summary>
    public bool Publish { get; set; } = true;
}