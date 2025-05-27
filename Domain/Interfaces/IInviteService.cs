using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IInviteService
{
    Task<Invites> SendInviteAsync(Guid eventId, Guid invitedUserId, Guid inviterUserId);
    Task RespondToInviteAsync(Guid inviteId, Guid eventId, Guid invitedId, bool accept);
    Task<Invites?> GetByIdAsync(Guid inviteId);
    Task<List<Invites>> GetPendingInvitesAsync(Guid userId);
}