using Domain;
using Domain.DTO;
using Domain.Interfaces;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class BoardColumnService: IBoardColumnService
{
    private readonly IBoardColumnRepository _repository;
    private readonly IEventRepository _eventRepository;

    public BoardColumnService(IBoardColumnRepository repository,  IEventRepository eventRepository)
    {
        _repository = repository;
        _eventRepository = eventRepository;
    }
    
    public async Task<ServiceResult<BoardColumn>> CreateColumnAsync(Guid eventId, string name, Guid userId)
    {
        var eventEntity = await _eventRepository.GetEventByIdAsync(eventId);
        if (eventEntity == null)
            return ServiceResult<BoardColumn>.Fail("Мероприятие не найдено");
        
        if (eventEntity.ResponsiblePersonId != userId)
            return ServiceResult<BoardColumn>.Fail("Вы не можете создать столбец");
        
        
        var Columns = await _repository.GetColumnsAsync(eventId);
        var maxOrder = Columns.Any() ? Columns.Max(t => t.Order) : 0;
        var column = new BoardColumn
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = name,
            Order = maxOrder + 1,
        };
        return ServiceResult<BoardColumn>.Ok(await _repository.AddColumnAsync(column));
    }

    public async Task<ServiceResult<List<BoardColumn>>> GetColumnsAsync(Guid eventId)
    {
        return ServiceResult<List<BoardColumn>>.Ok(await _repository.GetColumnsAsync(eventId));
    }

    public async Task<ServiceResult<BoardColumn?>> GetColumnByIdAsync(Guid columnId)
    {
        return ServiceResult<BoardColumn?>.Ok(await _repository.GetColumnByIdAsync(columnId));
    }

    public async Task<ServiceResult<BoardColumn>> UpdateColumnAsync(Guid columnId, BoardColumnUpdateRequest request, Guid userId)
    {
        var column = await _repository.GetColumnByIdAsync(columnId);
        if (column == null)
            return ServiceResult<BoardColumn>.Fail("Столбец не найден");
        
        var eventEntity = await _eventRepository.GetEventByIdAsync(column.EventId);
        if (eventEntity == null)
            return ServiceResult<BoardColumn>.Fail("Мероприятие не найдено");
        
        if (column.Event.ResponsiblePersonId != userId)
            return ServiceResult<BoardColumn>.Fail("Вы не можете изменять этот столбец");
        
        column.Name = request.Name ?? column.Name;
        column.Order = request.Order ?? column.Order;

        await _repository.UpdateColumnAsync(column);
        return ServiceResult<BoardColumn>.Ok(column);
    }

    public async Task<ServiceResult<bool>> DeleteColumnAsync(Guid columnId, Guid userId)
    {
        var column = await _repository.GetColumnByIdAsync(columnId);
        if (column == null)
            return ServiceResult<bool>.Fail("Удаляемого столбца не существует");

        var eventEntity = await _eventRepository.GetEventByIdAsync(column.EventId);
        if (eventEntity == null)
            return ServiceResult<bool>.Fail("Мероприятие для столбца не найдено");

        if (eventEntity.ResponsiblePersonId != userId)
            return ServiceResult<bool>.Fail("Вы не можете удалять этот столбец");

        await _repository.DeleteColumnAsync(column);
        return ServiceResult<bool>.Ok(true);
    }
}