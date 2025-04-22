namespace SEM.Domain.Models;

public class EventCategory
{
    public Guid EventId { get; set; }
    public Event Event { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; }
}