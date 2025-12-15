using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IInviteService
{
    Task<ServiceResult<Invites>> SendInviteAsync(Guid eventId, Guid invitedUserId, Guid inviterUserId);
    Task<ServiceResult<bool>> RespondToInviteAsync(Guid inviteId, Guid eventId, Guid invitedId, bool accept);
    Task<ServiceResult<Invites>> GetByIdAsync(Guid inviteId);
    Task<ServiceResult<List<Invites>>> GetPendingInvitesAsync(Guid userId);
    Task<ServiceResult<User>> GetInvitedUserAsync(Guid invitationId, Guid eventId);
    Task<ServiceResult<List<User>>> GetInvitedUsersAsync(Guid eventId, int count, int offset);
    Task<ServiceResult<bool>> DeleteInviteAsync(Guid invitationId, Guid eventId, Guid inviterId);
}