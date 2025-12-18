using Domain;
using Domain.DTO;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public EventService(IEventRepository eventRepository, IUserProfileRepository userProfileRepository)
    {
        _eventRepository = eventRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<ServiceResult<Event>> CreateEventAsync(EventRequest request, Guid modId)
    {
        var modUser = await _userProfileRepository.GetByIdAsync(modId);
        if (modUser.UserPrivilege == UserPrivilege.COMMON)
        {
            return ServiceResult<Event>.Fail("Вы не обладаете привилегиями для создания мероприятия");
        }
        var createdEvent = await _eventRepository.AddEventAsync(request);
        return ServiceResult<Event>.Ok(createdEvent);
    }

    public async Task<ServiceResult<Event>> GetEventByIdAsync(Guid eventId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        return _event == null 
            ? ServiceResult<Event>.Fail("Мероприятие не найдено") 
            : ServiceResult<Event>.Ok(_event);
    }

    public async Task<ServiceResult<List<Event>>> SearchEventsAsync(SearchRequest request)
    {
        var events = await _eventRepository.SearchEventsAsync(request);
        return ServiceResult<List<Event>>.Ok(events);
    }

    public async Task<ServiceResult<List<Category>>> GetAllCategoriesAsync()
    {
        var categories = await _eventRepository.GetAllCategoriesAsync();
        return ServiceResult<List<Category>>.Ok(categories);
    }

    public async Task<ServiceResult<bool>> DeleteEventAsync(Guid eventId, Guid userId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (_event.ResponsiblePersonId != userId)
            return ServiceResult<bool>.Fail("Вы не являетесь владельцем мероприятия");

        await _eventRepository.DeleteEventAndUnusedCategoriesAsync(_event);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> AddSuscriberAsync(Guid eventId, Guid userId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (_event.status == "FINISHED")
            return ServiceResult<bool>.Fail("Мероприятие завершено");

        await _eventRepository.AddSuscriberAsync(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteSuscriber(Guid eventId, Guid userId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (_event.status == "FINISHED")
            return ServiceResult<bool>.Fail("Мероприятие завершено");

        await _eventRepository.DeleteSuscriber(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<EventRole>> AddRoleToUser(Guid eventId, Guid userId, Guid roleId, Guid currentUserId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<EventRole>.Fail("Мероприятие не найдено");

        if (_event.ResponsiblePersonId != currentUserId)
            return ServiceResult<EventRole>.Fail("Вы не являетесь создателем мероприятия");
        if (_event.status == "FINISHED")
            return ServiceResult<EventRole>.Fail("Мероприятие завершено");

        var role = await _eventRepository.GetRoleByIdAsync(eventId, roleId);
        if (role == null)
            return ServiceResult<EventRole>.Fail("Роль не найдена");

        var addedRole = await _eventRepository.AddRoleToUser(eventId, userId, roleId);
        return ServiceResult<EventRole>.Ok(addedRole);
    }

    public async Task<ServiceResult<List<Roles>>> GetRolesByEvent(Guid eventId, int count, int offset)
    {
        var roles = await _eventRepository.GetRolesByEvent(eventId, count, offset);
        return ServiceResult<List<Roles>>.Ok(roles);
    }

    public async Task<ServiceResult<List<EventUserResponse>>> GetAllSuscribersAsync(Guid eventId, string? name, int count, int offset)
    {
        var subs = await _eventRepository.GetAllSuscribersAsync(eventId, name, count, offset);
        return ServiceResult<List<EventUserResponse>>.Ok(subs);
    }
    
    public async Task<ServiceResult<List<EventUserResponse>>> GetAllSuscribersWithoutOffsetAsync(Guid eventId, string? name)
    {
        var subs = await _eventRepository.GetAllSuscribersWithoutOffsetAsync(eventId, name);
        return ServiceResult<List<EventUserResponse>>.Ok(subs);
    }

    public async Task<ServiceResult<Event>> UpdateEventAsync(Guid eventId, EventUpdateRequest request, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Event>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<Event>.Fail("Вы не являетесь создателем мероприятия");
        
        if (@curEvent.status == "FINISHED")
            return ServiceResult<Event>.Fail("Мероприятие завершено");

        curEvent.Name = request.Name ?? curEvent.Name;
        curEvent.Description = request.Description ?? curEvent.Description;
        curEvent.StartDate = request.StartDate ?? curEvent.StartDate;
        curEvent.EndDate = request.EndDate ?? curEvent.EndDate;
        curEvent.Location = request.Location ?? curEvent.Location;
        curEvent.Format = request.Format ?? curEvent.Format;
        curEvent.EventType = request.EventType ?? curEvent.EventType;
        curEvent.MaxParticipants = request.MaxParticipants ?? curEvent.MaxParticipants;

        var updatedEvent = await _eventRepository.UpdateEventAsync(curEvent);
        return ServiceResult<Event>.Ok(updatedEvent);
    }

    public async Task<ServiceResult<List<PhotoResponse>>> GetEventPhotosAsync(Guid eventId, int count, int offset)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<List<PhotoResponse>>.Fail("Мероприятие не найдено");

        var photos = await _eventRepository.GetEventPhotosAsync(eventId, offset, count);
        return ServiceResult<List<PhotoResponse>>.Ok(photos);
    }
    
    public async Task<ServiceResult<string>> AddEventPhotoAsync(Guid eventId, IFormFile file)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<string>.Fail("Мероприятие не найдено");

        if (file == null || file.Length == 0)
            return ServiceResult<string>.Fail("Файл не загружен");
        
        if (_event.status == "FINISHED")
            return ServiceResult<string>.Fail("Мероприятие завершено");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = "/" + Path.Combine("uploads", fileName).Replace("\\", "/");
        await _eventRepository.AddEventPhotoAsync(new EventPhoto { Id = Guid.NewGuid(), EventId = eventId, FilePath = relativePath });

        return ServiceResult<string>.Ok(relativePath);
    }

    public async Task<ServiceResult<bool>> AddContact(Guid eventId, Guid userId, Guid currentUserId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != currentUserId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event.status == "FINISHED")
            return ServiceResult<bool>.Fail("Мероприятие завершено");
        await _eventRepository.AddContact(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<List<ContactResponse>>> GetContacts(Guid eventId)
    {
        var contacts = await _eventRepository.GetContacts(eventId);
        return ServiceResult<List<ContactResponse>>.Ok(contacts);
    }

    public async Task<ServiceResult<Roles>> GetRoleByName(string roleName)
    {
        var role = await _eventRepository.GetRoleByName(roleName);
        return role == null 
            ? ServiceResult<Roles>.Fail("Роль не найдена") 
            : ServiceResult<Roles>.Ok(role);
    }

    public async Task<ServiceResult<Event>> FinishEventAsync(Guid eventId, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Event>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<Event>.Fail("Вы не являетесь создателем мероприятия");
        
        curEvent.status = "FINISHED";
        var updatedEvent = await _eventRepository.FinishEventAsync(curEvent);
        return ServiceResult<Event>.Ok(updatedEvent);
    }

    public async Task<ServiceResult<Roles>> CreateRoleAsync(string roleName, Guid eventId, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Roles>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<Roles>.Fail("Вы не являетесь создателем мероприятия");
        if (curEvent.status == "FINISHED")
            return ServiceResult<Roles>.Fail("Мероприятие завершено");
        var role = new Roles
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            EventId = eventId
        };
        await _eventRepository.CreateRoleAsync(role);
        return ServiceResult<Roles>.Ok(role);
    }

    public async Task<ServiceResult<Roles>> GetRoleByIdAsync(Guid eventId, Guid roleId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Roles>.Fail("Мероприятие не найдено");
        var role = await _eventRepository.GetRoleByIdAsync(eventId, roleId);
        if (role == null)
            return ServiceResult<Roles>.Fail("Роль не найдена");
        return ServiceResult<Roles>.Ok(role);
    }

    public async Task<ServiceResult<Roles>> UpdateRoleAsync(string newRoleName, Guid eventId, Guid roleId, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Roles>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<Roles>.Fail("Вы не являетесь создателем мероприятия");
        if (curEvent.status == "FINISHED")
            return ServiceResult<Roles>.Fail("Мероприятие завершено");
        var role = await _eventRepository.GetRoleByIdAsync(eventId, roleId); 
        role.Name = newRoleName;
        await _eventRepository.UpdateRoleAsync(role);
        return ServiceResult<Roles>.Ok(role);
    }

    public async Task<ServiceResult<bool>> DeleteRoleAsync(Guid eventId, Guid roleId, Guid  userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        if (curEvent.status == "FINISHED")
            return ServiceResult<bool>.Fail("Мероприятие завершено");
        
        await _eventRepository.DeleteRoleAsync(eventId, roleId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<string>> GetEventPhotoByIdAsync(Guid eventId, Guid photoId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<string>.Fail("Мероприятие не найдено");
        var res = await _eventRepository.GetEventPhotoByIdAsync(eventId, photoId);
        if (res == null)
            return ServiceResult<string>.Ok("Фото не найдено");
        return ServiceResult<string>.Ok(res);
    }

    public async Task<ServiceResult<bool>> DeleteEventPhotoAsync(Guid eventId, Guid photoId, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        await _eventRepository.DeleteEventPhotoAsync(eventId, photoId);
        return ServiceResult<bool>.Ok(true);
    }
    
    public async Task<ServiceResult<bool>> DeleteContact(Guid eventId, Guid userId, Guid currentUserId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != currentUserId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event.status == "FINISHED")
            return ServiceResult<bool>.Fail("Мероприятие завершено");
        if (userId == curEvent.ResponsiblePersonId)
            return ServiceResult<bool>.Fail("Мероприятие не может остаться без создателя/организатора");
        await _eventRepository.DeleteContact(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }
}