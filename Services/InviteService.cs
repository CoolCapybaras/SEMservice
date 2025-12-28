using System.Text.Json;
using Domain;
using Domain.DTO;
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

    public async Task<ServiceResult<Invites>> SendInviteAsync(Guid eventId, Guid invitedUserId, Guid inviterUserId)
    {
        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<Invites>.Fail("Мероприятие не найдено");

        var user = await _profileRepository.GetByIdAsync(inviterUserId);
        if (user == null)
            return ServiceResult<Invites>.Fail("Пользователь не найден");
        if (@event.status == "FINISHED")
            return ServiceResult<Invites>.Fail("Мероприятие завершено");
        
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

        var payload = new
        {
            invite_id = invite.Id,
            community_name = @event.Name,
            inviter_username = $"{user.LastName} {user.FirstName}" 
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

        return ServiceResult<Invites>.Ok(invite);
    }

    public async Task<ServiceResult<bool>> RespondToInviteAsync(Guid inviteId, Guid eventId, Guid invitedId, bool accept)
    {
        if (accept)
        {
            await _inviteRepository.AcceptInviteAsync(inviteId);
            await _eventRepository.AddSuscriberAsync(eventId, invitedId);
        }
        else
            await _inviteRepository.DeclineInviteAsync(inviteId);
        
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<Invites>> GetByIdAsync(Guid inviteId)
    {
        var invite = await _inviteRepository.GetByIdAsync(inviteId);
        return invite == null 
            ? ServiceResult<Invites>.Fail("Приглашение не найдено") 
            : ServiceResult<Invites>.Ok(invite);
    }

    public async Task<ServiceResult<List<Invites>>> GetPendingInvitesAsync(Guid userId)
    {
        var invites = await _inviteRepository.GetUserInvitesAsync(userId);
        var pending = invites.Where(i => i.Status == InviteStatus.Pending).ToList();
        return ServiceResult<List<Invites>>.Ok(pending);
    }

    public async Task<ServiceResult<User>> GetInvitedUserAsync(Guid invitationId, Guid eventId)
    {
        var invite = await _inviteRepository.GetByIdAsync(invitationId);
        if (invite.Status != InviteStatus.Pending)
            return ServiceResult<User>.Fail("Приглашение уже принято/отклонено");
        var user = await _inviteRepository.GetInvitedUserAsync(invitationId, eventId);
        if (user == null)
            return ServiceResult<User>.Fail("Приглашение или ивент не найдены");
        return ServiceResult<User>.Ok(MapToModel(user));
        
    }

    public async Task<ServiceResult<List<User>>> GetInvitedUsersAsync(Guid eventId, int count, int offset)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<List<User>>.Fail("Мероприятие не найдено");
        var invites = await _inviteRepository.GetUserInvitesAsync(eventId);
        if (invites.Count == 0)
            return ServiceResult<List<User>>.Fail("Нет действующих приглашений");
        var users = await _inviteRepository.GetInvitedUsersAsync(eventId, count, offset);
        var result = new List<User>();
        foreach (var user in users)
        {
            result.Add(MapToModel(user));
        }
        return ServiceResult<List<User>>.Ok(result);
            
    }

    public async Task<ServiceResult<bool>> DeleteInviteAsync(Guid invitationId, Guid eventId, Guid inviterId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        var invite = await _inviteRepository.GetByIdAsync(invitationId);
        if (invite.Status != InviteStatus.Pending)
            return ServiceResult<bool>.Fail("На приглашение уже ответили");
        if (invite.InviterId != inviterId)
            return ServiceResult<bool>.Fail("Не вы отправили приглашение");
        await _inviteRepository.DeleteInviteAsync(invitationId, eventId);
        return ServiceResult<bool>.Ok(true);
    }

    private static User MapToModel(User profile)
    {
        return new User
        {
            Id = profile.Id,
            LastName = profile.LastName,
            FirstName = profile.FirstName,
            Profession = profile.Profession,
            PhoneNumber = profile.PhoneNumber,
            Telegram = profile.Telegram,
            City = profile.City,
            AvatarUrl = profile.AvatarUrl
        };
    }
}