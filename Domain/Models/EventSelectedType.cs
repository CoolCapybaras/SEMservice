namespace SEM.Domain.Models;

/// <summary>Связь мероприятия с выбранным типом (множественный выбор).</summary>
public class EventSelectedType
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public EventTypeKind TypeKind { get; set; }
}