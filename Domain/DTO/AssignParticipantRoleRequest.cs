using SEM.Domain.Models;

namespace Domain.DTO;

public class AssignParticipantRoleRequest
{
    public ParticipantRoleKind ParticipantRole { get; set; }
}