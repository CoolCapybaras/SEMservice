using System.Text.Json;
using Domain.Interfaces;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class InviteService : IInviteService
{
    private readonly IInviteRepository _inviteRepository;
    private readonly INotificationService _notificationService;
    private readonly IEventRepository _eventRepository;
    private readonly IUserProfileRepository _profileRepository;

    public InviteService(IInviteRepository inviteRepository, INotificationService notificationService, IEventRepository eventRepository, IUserProfileRepository userProfileRepository)
    {
        _inviteRepository = inviteRepository;
        _notificationService = notificationService;
        _eventRepository = eventRepository;
        _profileRepository = userProfileRepository;
    }

    public async Task<Invites> SendInviteAsync(Guid eventId, Guid invitedUserId, Guid inviterUserId)
    {
        var invite = new Invites
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            InvitedUserId = invitedUserId,
            InviterId = inviterUserId,
            Status = InviteStatus.Pending,
            InvitedAt = DateTime.UtcNow
        };

        await _inviteRepository.AddInviteAsync(invite);

        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        var user = await _profileRepository.GetByIdAsync(inviterUserId);

        var payload = new
        {
            invite_id = invite.Id,
            community_name = @event.Name,
            inviter_username = $"{user.LastName} {user.FirstName} {user.MiddleName}" 
        };

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = invitedUserId,
            Type = "Invite",
            Payload = JsonSerializer.Serialize(payload),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationService.AddNotificationAsync(notification);

        return invite;
    }

    public async Task RespondToInviteAsync(Guid inviteId, Guid eventId, Guid invitedId, bool accept)
    {
        if (accept)
        {
            await _inviteRepository.AcceptInviteAsync(inviteId);
            await _eventRepository.AddSuscriberAsync(eventId, invitedId);
        }
        else
            await _inviteRepository.DeclineInviteAsync(inviteId);
    }

    public async Task<Invites?> GetByIdAsync(Guid inviteId)
    {
        return await _inviteRepository.GetByIdAsync(inviteId);
    }

    public async Task<List<Invites>> GetPendingInvitesAsync(Guid userId)
    {
        var invites = await _inviteRepository.GetUserInvitesAsync(userId);
        return invites.Where(i => i.Status == InviteStatus.Pending).ToList();
    }
}