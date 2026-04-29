namespace SEM.Domain.Models;

public class EventRole
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ParticipantRoleKind ParticipantRole { get; set; }

    public bool IsContact { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}