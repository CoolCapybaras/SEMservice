using SEM.Domain.Models;

namespace Domain.DTO;

public class EventParticipantAssignmentDto
{
    public Guid UserId { get; set; }
    public ParticipantRoleKind Role { get; set; }
}
