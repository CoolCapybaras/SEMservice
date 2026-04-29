using Domain;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Services.Hubs;

namespace SEM.Services;

public class BoardService: IBoardService
{
    private readonly IBoardTaskRepository _taskRepository;
    private readonly IBoardTaskHistoryRepository _historyRepository;
    private readonly IBoardColumnRepository _columnRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IHubContext<BoardHub> _boardHubContext;

    public BoardService(
        IBoardTaskRepository taskRepository,
        IBoardTaskHistoryRepository historyRepository,
        IBoardColumnRepository columnRepository,
        IEventRepository eventRepository,
        IHubContext<BoardHub> boardHubContext)
    {
        _taskRepository = taskRepository;
        _historyRepository = historyRepository;
        _columnRepository = columnRepository;
        _eventRepository = eventRepository;
        _boardHubContext = boardHubContext;
    }

    public async Task<ServiceResult<List<BoardDto>>> GetBoardAsync(Guid eventId)
    {
        
        var columns = await _columnRepository.GetColumnsAsync(eventId);
        var boardDto = new BoardDto
        {
            EventId = eventId,
            Columns = columns.OrderBy(c => c.Order).Select(c => new BoardColumnDto
            {
                Id = c.Id,
                Name = c.Name,
                Order = c.Order,
                Tasks = c.Tasks.OrderBy(t => t.Order).Select(t => new BoardTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    AssignedUserId = t.AssignedUserId,
                    CreatorId = t.CreatorId,
                    DueDate = t.DueDate,
                    Order = t.Order,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    Status = c.Name
                }).ToList()
            }).ToList()
        };

        return ServiceResult<List<BoardDto>>.Ok(new List<BoardDto> { boardDto });
    }

    public async Task<ServiceResult<BoardTask>> MoveTaskAsync(Guid taskId, MoveTaskRequest request, Guid userId)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<BoardTask>.Fail("Задача не найдена");
        
        var eventEntity = await _eventRepository.GetEventByIdAsync(task.Column.EventId);
        if (eventEntity == null)
            return ServiceResult<BoardTask>.Fail("Мероприятие не найдено");

        var role = GetRoleForUser(eventEntity, userId);
        var canMove = userId == eventEntity.ResponsiblePersonId
                      || role == ParticipantRoleKind.Editor
                      || role == ParticipantRoleKind.Assistant;
        if (!canMove)
            return ServiceResult<BoardTask>.Fail("У вас нет прав для изменения статуса задачи");
        if (eventEntity.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<BoardTask>.Fail("Мероприятие в архиве");
        
        var oldColumnName = task.Column.Name;

        var oldColumnId = task.ColumnId;
        var newColumnId = request.TargetColumnId;
        int newOrder = request.NewOrder;

        bool sameColumn = oldColumnId == newColumnId;

        var oldTasks = await _taskRepository.GetTasksByColumnIdAsync(oldColumnId);
        oldTasks = oldTasks.OrderBy(t => t.Order).ToList();

        var newTasks = sameColumn
            ? oldTasks
            : (await _taskRepository.GetTasksByColumnIdAsync(newColumnId)).OrderBy(t => t.Order).ToList();

        // Удаляем из старой
        oldTasks.Remove(task);

        if (!sameColumn)
        {
            for (int i = 0; i < oldTasks.Count; i++)
                oldTasks[i].Order = i;
        }

        // Вставляем в новую
        if (!sameColumn)
            task.ColumnId = newColumnId;

        // правильный newOrder
        if (newOrder < 0) newOrder = 0;
        if (newOrder > newTasks.Count) newOrder = newTasks.Count;

        newTasks.Insert(newOrder, task);

        // переиндексация новой
        for (int i = 0; i < newTasks.Count; i++)
            newTasks[i].Order = i;

        task.UpdatedAt = DateTime.UtcNow;
        
        await _taskRepository.MoveTasksAsync(oldTasks, newTasks, task);
        var targetColumn = await _columnRepository.GetColumnByIdAsync(newColumnId);
        await _historyRepository.AddAsync(new BoardTaskHistory
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ChangedByUserId = userId,
            Action = "TaskMoved",
            FieldName = "Status",
            OldValue = oldColumnName,
            NewValue = targetColumn?.Name
        });

        await _boardHubContext.Clients.Group(eventEntity.Id.ToString()).SendAsync("TaskMoved", new
        {
            taskId = task.Id,
            fromColumnId = oldColumnId,
            toColumnId = newColumnId,
            newOrder
        });

        return ServiceResult<BoardTask>.Ok(task);
    }

    private static ParticipantRoleKind? GetRoleForUser(Event eventEntity, Guid userId)
    {
        if (eventEntity.ResponsiblePersonId == userId)
            return ParticipantRoleKind.Organizer;
        return eventEntity.EventRoles.FirstOrDefault(x => x.UserId == userId)?.ParticipantRole;
    }
}