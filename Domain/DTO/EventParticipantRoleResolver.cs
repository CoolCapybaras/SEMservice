namespace SEM.Domain.Models;

public static class EventParticipantRoleResolver
{
    public static ParticipantRoleKind? Resolve(Event evt, Guid userId)
    {
        if (evt.ResponsiblePersonId == userId)
            return ParticipantRoleKind.Organizer;

        return evt.EventRoles?.FirstOrDefault(r => r.UserId == userId)?.ParticipantRole;
    }

    public static void ApplyMyRole(IEnumerable<Event> events, Guid userId)
    {
        foreach (var evt in events)
            evt.MyParticipantRole = Resolve(evt, userId);
    }
}