using Domain;
using Domain.DTO;
using Domain.Interfaces;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class EventNoteService : IEventNoteService
{
    private readonly IEventNoteRepository _noteRepository;
    private readonly IEventRepository _eventRepository;

    public EventNoteService(IEventNoteRepository noteRepository, IEventRepository eventRepository)
    {
        _noteRepository = noteRepository;
        _eventRepository = eventRepository;
    }

    public async Task<ServiceResult<EventNoteResponse>> CreateAsync(Guid eventId, Guid userId, string text)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<EventNoteResponse>.Fail("Мероприятие не найдено");
        if (!CanCreateOrUpdate(evt, userId))
            return ServiceResult<EventNoteResponse>.Fail("Недостаточно прав для создания заметки");
        if (evt.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<EventNoteResponse>.Fail("Архивное мероприятие доступно только для чтения");
        if (string.IsNullOrWhiteSpace(text))
            return ServiceResult<EventNoteResponse>.Fail("Текст заметки пустой");

        var note = new EventNote
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AuthorId = userId,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _noteRepository.AddAsync(note);
        var created = await _noteRepository.GetByIdAsync(note.Id) ?? note;
        return ServiceResult<EventNoteResponse>.Ok(ToResponse(created));
    }

    public async Task<ServiceResult<EventNoteResponse>> UpdateAsync(Guid eventId, Guid noteId, Guid userId, string text)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<EventNoteResponse>.Fail("Мероприятие не найдено");
        if (evt.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<EventNoteResponse>.Fail("Архивное мероприятие доступно только для чтения");
        if (string.IsNullOrWhiteSpace(text))
            return ServiceResult<EventNoteResponse>.Fail("Текст заметки пустой");

        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note == null || note.EventId != eventId)
            return ServiceResult<EventNoteResponse>.Fail("Заметка не найдена");
        if (note.AuthorId != userId)
            return ServiceResult<EventNoteResponse>.Fail("Редактировать заметку может только автор");

        note.Text = text.Trim();
        note.UpdatedAt = DateTime.UtcNow;
        var updated = await _noteRepository.UpdateAsync(note);
        return ServiceResult<EventNoteResponse>.Ok(ToResponse(updated));
    }

    public async Task<ServiceResult<List<EventNoteResponse>>> GetByEventAsync(Guid eventId, Guid userId)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<List<EventNoteResponse>>.Fail("Мероприятие не найдено");
        if (!IsParticipant(evt, userId))
            return ServiceResult<List<EventNoteResponse>>.Fail("Вы не являетесь участником мероприятия");

        var notes = await _noteRepository.GetByEventIdAsync(eventId);
        return ServiceResult<List<EventNoteResponse>>.Ok(notes.Select(ToResponse).ToList());
    }
    
    public async Task<ServiceResult<bool>> DeleteAsync(Guid eventId, Guid noteId, Guid userId)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (evt.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<bool>.Fail("Архивное мероприятие доступно только для чтения");

        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note == null || note.EventId != eventId)
            return ServiceResult<bool>.Fail("Заметка не найдена");
        if (note.AuthorId != userId)
            return ServiceResult<bool>.Fail("Удалить заметку может только автор");

        await _noteRepository.DeleteAsync(note);
        return ServiceResult<bool>.Ok(true);
    }

    private static EventNoteResponse ToResponse(EventNote note)
    {
        return new EventNoteResponse
        {
            Id = note.Id,
            EventId = note.EventId,
            AuthorId = note.AuthorId,
            AuthorName = $"{note.Author?.LastName} {note.Author?.FirstName}".Trim(),
            Text = note.Text,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }

    private static bool IsParticipant(Event evt, Guid userId) =>
        userId == evt.ResponsiblePersonId || evt.EventRoles.Any(r => r.UserId == userId);

    private static bool CanCreateOrUpdate(Event evt, Guid userId)
    {
        if (userId == evt.ResponsiblePersonId)
            return true;
        var role = evt.EventRoles.FirstOrDefault(r => r.UserId == userId)?.ParticipantRole;
        return role == ParticipantRoleKind.Editor || role == ParticipantRoleKind.Assistant;
    }
}
