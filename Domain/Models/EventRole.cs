namespace SEM.Domain.Models;

public class EventRole
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Roles Role { get; set; } = null!;

    /// <summary>Дублирует смысл Role для быстрых проверок и API; синхронизируется с одной из четырёх фиксированных ролей события.</summary>
    public ParticipantRoleKind ParticipantRole { get; set; }

    public bool IsContact { get; set; }
}