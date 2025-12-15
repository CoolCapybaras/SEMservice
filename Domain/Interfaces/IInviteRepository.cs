using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IInviteRepository
{
    Task<Invites> AddInviteAsync(Invites invite);
    Task<Invites?> GetByIdAsync(Guid inviteId);
    Task<List<Invites>> GetUserInvitesAsync(Guid userId);
    Task AcceptInviteAsync(Guid inviteId);
    Task DeclineInviteAsync(Guid inviteId);
    Task<User> GetInvitedUserAsync(Guid invitationId, Guid eventId);
    Task<List<User>> GetInvitedUsersAsync(Guid eventId, int count, int offset);
    Task DeleteInviteAsync(Guid invitationId, Guid eventId);
}