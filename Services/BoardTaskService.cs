using Domain;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Services.Hubs;

namespace SEM.Services;

public class BoardTaskService : IBoardTaskService
{
    private readonly IBoardTaskRepository _repository;
    private readonly IBoardTaskCommentRepository _commentRepository;
    private readonly IBoardTaskHistoryRepository _historyRepository;
    private readonly IBoardColumnRepository _columnRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IHubContext<BoardHub> _boardHubContext;

    public BoardTaskService(
        IBoardTaskRepository repository,
        IBoardTaskCommentRepository commentRepository,
        IBoardTaskHistoryRepository historyRepository,
        IBoardColumnRepository columnRepository,
        IEventRepository eventRepository,
        IHubContext<BoardHub> boardHubContext)
    {
        _repository = repository;
        _commentRepository = commentRepository;
        _historyRepository = historyRepository;
        _columnRepository = columnRepository;
        _eventRepository = eventRepository;
        _boardHubContext = boardHubContext;
    }

    public async Task<ServiceResult<BoardTaskDto>> CreateTaskAsync(Guid columnId, string title, string? description, Guid? assigneeId, DateTime? deadline,
        Guid userId, BoardTaskPriority? priority = null)
    {
        var column = await _columnRepository.GetColumnByIdAsync(columnId);
        if (column == null)
            return ServiceResult<BoardTaskDto>.Fail("Столбец не найден");

        var eventEntity = await _eventRepository.GetEventByIdAsync(column.EventId);
        if (eventEntity == null)
            return ServiceResult<BoardTaskDto>.Fail("Мероприятие не найдено");
        if (eventEntity.LifecycleState is EventLifecycleState.Completed or EventLifecycleState.Archived)
            return ServiceResult<BoardTaskDto>.Fail("Создание новых задач недоступно для завершенного/архивного мероприятия");

        var currentRole = GetRoleForUser(eventEntity, userId);
        if (userId != eventEntity.ResponsiblePersonId &&
            currentRole != ParticipantRoleKind.Editor)
            return ServiceResult<BoardTaskDto>.Fail("Вы не можете добавлять задачи в этот столбец");

        if (assigneeId.HasValue)
        {
            var assigneeRole = GetRoleForUser(eventEntity, assigneeId.Value);
            if (assigneeRole != ParticipantRoleKind.Editor && assigneeRole != ParticipantRoleKind.Assistant)
                return ServiceResult<BoardTaskDto>.Fail("Назначить задачу можно только редактору или помощнику");
        }
        
        var tasksInColumn = await _repository.GetTasksByColumnIdAsync(columnId);
        var maxOrder = tasksInColumn.Any() ? tasksInColumn.Max(t => t.Order) : 0;

        var task = new BoardTask
        {
            Id = Guid.NewGuid(),
            ColumnId = columnId,
            Title = title,
            Description = description,
            AssignedUserId = assigneeId,
            CreatorId = userId,
            DueDate = deadline,
            Priority = priority ?? BoardTaskPriority.Medium,
            Order = maxOrder + 1
        };

        await _repository.AddTaskAsync(task);
        await _historyRepository.AddAsync(new BoardTaskHistory
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ChangedByUserId = userId,
            Action = "TaskCreated",
            NewValue = title
        });
        var created = await _repository.GetTaskByIdAsync(task.Id) ?? task;
        await _boardHubContext.Clients.Group(eventEntity.Id.ToString()).SendAsync("TaskCreated", ToDto(created, column.Name, 0));
        return ServiceResult<BoardTaskDto>.Ok(ToDto(created, column.Name, 0));
    }

    public async Task<ServiceResult<List<BoardTaskDto>>> GetTasksAsync(Guid columnId)
    {
        var tasks = await _repository.GetTasksByColumnIdAsync(columnId);
        var column = await _columnRepository.GetColumnByIdAsync(columnId);
        var counts = await _commentRepository.GetCommentCountsByTaskIdsAsync(tasks.Select(t => t.Id).ToList());
        var dtoList = tasks.Select(t => ToDto(t, column!.Name, counts.GetValueOrDefault(t.Id))).ToList();
        return ServiceResult<List<BoardTaskDto>>.Ok(dtoList);
    }

    public async Task<ServiceResult<List<BoardTasksResponse>>> GetCurrentUserTasksAsync(Guid userId)
    {
        var tasks = await _repository.GetCurrentUserTasksAsync(userId);
        var dtoList = tasks.Select(ToMyTaskDto).ToList();

        return ServiceResult<List<BoardTasksResponse>>.Ok(dtoList);
    }

    public async Task<ServiceResult<List<BoardTasksResponse>>> GetCurrentUserTasksByEventAsync(Guid eventId, Guid userId)
    {
        var eventEntity = await _eventRepository.GetEventByIdAsync(eventId);
        if (eventEntity == null)
            return ServiceResult<List<BoardTasksResponse>>.Fail("Мероприятие не найдено");

        var isParticipant = userId == eventEntity.ResponsiblePersonId || eventEntity.EventRoles.Any(r => r.UserId == userId);
        if (!isParticipant)
            return ServiceResult<List<BoardTasksResponse>>.Fail("Вы не являетесь участником мероприятия");

        var tasks = await _repository.GetCurrentUserTasksByEventAsync(userId, eventId);
        var dtoList = tasks.Select(ToMyTaskDto).ToList();
        return ServiceResult<List<BoardTasksResponse>>.Ok(dtoList);
    }

    public async Task<ServiceResult<BoardTaskDto?>> GetTaskByIdAsync(Guid taskId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<BoardTaskDto?>.Ok(null);
        var column = await _columnRepository.GetColumnByIdAsync(task.ColumnId);
        if (column == null)
            return ServiceResult<BoardTaskDto?>.Ok(null);
        var counts = await _commentRepository.GetCommentCountsByTaskIdsAsync(new[] { task.Id });
        return ServiceResult<BoardTaskDto?>.Ok(ToDto(task, column.Name, counts.GetValueOrDefault(task.Id)));
    }

    public async Task<ServiceResult<BoardTaskDto>> UpdateTaskAsync(Guid taskId, BoardTaskUpdateRequest request, Guid userId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<BoardTaskDto>.Fail("Задача не найдена");

        var column = await _columnRepository.GetColumnByIdAsync(task.ColumnId);
        var eventEntity = await _eventRepository.GetEventByIdAsync(column.EventId);
        if (eventEntity == null)
            return ServiceResult<BoardTaskDto>.Fail("Мероприятие не найдено");

        var currentRole = GetRoleForUser(eventEntity, userId);
        var canEditTask = userId == eventEntity.ResponsiblePersonId
                          || currentRole == ParticipantRoleKind.Editor
                          || currentRole == ParticipantRoleKind.Assistant;
        if (!canEditTask)
            return ServiceResult<BoardTaskDto>.Fail("Вы не можете изменять эту задачу");

        if (request.AssigneeId.HasValue)
        {
            var assigneeRole = GetRoleForUser(eventEntity, request.AssigneeId.Value);
            if (assigneeRole != ParticipantRoleKind.Editor && assigneeRole != ParticipantRoleKind.Assistant)
                return ServiceResult<BoardTaskDto>.Fail("Назначить задачу можно только редактору или помощнику");
        }

        var historyEntries = new List<BoardTaskHistory>();

        if (request.Title != null && request.Title != task.Title)
        {
            historyEntries.Add(CreateHistory(task.Id, userId, "TaskUpdated", "Title", task.Title, request.Title));
            task.Title = request.Title;
        }
        if (request.Description != null && request.Description != task.Description)
        {
            historyEntries.Add(CreateHistory(task.Id, userId, "TaskUpdated", "Description", task.Description, request.Description));
            task.Description = request.Description;
        }
        if (request.AssigneeId.HasValue && request.AssigneeId != task.AssignedUserId)
        {
            historyEntries.Add(CreateHistory(task.Id, userId, "TaskUpdated", "Assignee", task.AssignedUserId?.ToString(), request.AssigneeId.Value.ToString()));
            task.AssignedUserId = request.AssigneeId.Value;
        }
        if (request.Deadline.HasValue && request.Deadline != task.DueDate)
        {
            historyEntries.Add(CreateHistory(task.Id, userId, "TaskUpdated", "Deadline", task.DueDate?.ToString("O"), request.Deadline.Value.ToString("O")));
            task.DueDate = request.Deadline.Value;
            task.DeadlineReminderSentAt = null;
            task.OverdueNotificationSentAt = null;
        }
        if (request.Priority.HasValue && request.Priority.Value != task.Priority)
        {
            historyEntries.Add(CreateHistory(task.Id, userId, "TaskUpdated", "Priority", task.Priority.ToString(), request.Priority.Value.ToString()));
            task.Priority = request.Priority.Value;
        }

        task.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateTaskAsync(task);
        if (historyEntries.Count > 0)
            await _historyRepository.AddRangeAsync(historyEntries);

        var counts = await _commentRepository.GetCommentCountsByTaskIdsAsync(new[] { task.Id });
        var dto = ToDto(task, column.Name, counts.GetValueOrDefault(task.Id));
        await _boardHubContext.Clients.Group(eventEntity.Id.ToString()).SendAsync("TaskUpdated", dto);
        return ServiceResult<BoardTaskDto>.Ok(dto);
    }

    public async Task<ServiceResult<bool>> DeleteTaskAsync(Guid taskId, Guid userId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<bool>.Fail("Задача не найдена");

        var column = await _columnRepository.GetColumnByIdAsync(task.ColumnId);
        var eventEntity = await _eventRepository.GetEventByIdAsync(column.EventId);
        if (eventEntity == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        var currentRole = GetRoleForUser(eventEntity, userId);
        var canDeleteTask = userId == eventEntity.ResponsiblePersonId
                            || (currentRole == ParticipantRoleKind.Editor && (!task.DueDate.HasValue || task.DueDate.Value > DateTime.UtcNow));
        if (!canDeleteTask)
            return ServiceResult<bool>.Fail("Вы не можете удалять эту задачу");

        await _repository.DeleteTaskAsync(task);
        await _boardHubContext.Clients.Group(eventEntity.Id.ToString()).SendAsync("TaskDeleted", new { taskId });
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<BoardTaskCommentResponse>> AddCommentAsync(Guid taskId, Guid userId, string text)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<BoardTaskCommentResponse>.Fail("Задача не найдена");
        var eventEntity = await _eventRepository.GetEventByIdAsync(task.Column.EventId);
        if (eventEntity == null)
            return ServiceResult<BoardTaskCommentResponse>.Fail("Мероприятие не найдено");
        if (eventEntity.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<BoardTaskCommentResponse>.Fail("Мероприятие в архиве");

        var role = GetRoleForUser(eventEntity, userId);
        var canComment = userId == eventEntity.ResponsiblePersonId
                         || role == ParticipantRoleKind.Editor
                         || role == ParticipantRoleKind.Assistant;
        if (!canComment)
            return ServiceResult<BoardTaskCommentResponse>.Fail("У вас нет прав на комментирование задач");
        if (string.IsNullOrWhiteSpace(text))
            return ServiceResult<BoardTaskCommentResponse>.Fail("Комментарий пустой");

        var entity = new BoardTaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            AuthorId = userId,
            Text = text.Trim()
        };
        await _commentRepository.AddAsync(entity);
        await _historyRepository.AddAsync(new BoardTaskHistory
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ChangedByUserId = userId,
            Action = "CommentAdded",
            NewValue = text.Trim()
        });
        var comments = await _commentRepository.GetByTaskIdAsync(taskId);
        var added = comments.Last(c => c.Id == entity.Id);
        return ServiceResult<BoardTaskCommentResponse>.Ok(ToCommentDto(added));
    }

    public async Task<ServiceResult<List<BoardTaskCommentResponse>>> GetCommentsAsync(Guid taskId, Guid userId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<List<BoardTaskCommentResponse>>.Fail("Задача не найдена");
        var eventEntity = await _eventRepository.GetEventByIdAsync(task.Column.EventId);
        if (eventEntity == null)
            return ServiceResult<List<BoardTaskCommentResponse>>.Fail("Мероприятие не найдено");

        var isParticipant = userId == eventEntity.ResponsiblePersonId || eventEntity.EventRoles.Any(r => r.UserId == userId);
        if (!isParticipant)
            return ServiceResult<List<BoardTaskCommentResponse>>.Fail("Вы не являетесь участником мероприятия");

        var comments = await _commentRepository.GetByTaskIdAsync(taskId);
        return ServiceResult<List<BoardTaskCommentResponse>>.Ok(comments.Select(ToCommentDto).ToList());
    }
    
    public async Task<ServiceResult<List<BoardTaskHistoryResponse>>> GetHistoryAsync(Guid taskId, Guid userId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<List<BoardTaskHistoryResponse>>.Fail("Задача не найдена");
        var eventEntity = await _eventRepository.GetEventByIdAsync(task.Column.EventId);
        if (eventEntity == null)
            return ServiceResult<List<BoardTaskHistoryResponse>>.Fail("Мероприятие не найдено");

        var isParticipant = userId == eventEntity.ResponsiblePersonId || eventEntity.EventRoles.Any(r => r.UserId == userId);
        if (!isParticipant)
            return ServiceResult<List<BoardTaskHistoryResponse>>.Fail("Вы не являетесь участником мероприятия");

        var history = await _historyRepository.GetByTaskIdAsync(taskId);
        return ServiceResult<List<BoardTaskHistoryResponse>>.Ok(history.Select(ToHistoryDto).ToList());
    }
    
    private static string UserDisplayName(User u)
    {
        var parts = new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim());
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? (u.Email ?? u.Id.ToString()) : name;
    }

    private BoardTaskDto ToDto(BoardTask task, string status, int commentCount)
    {
        return new BoardTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            AssignedUserId = task.AssignedUserId,
            AssigneeDisplayName = task.AssignedUser != null ? UserDisplayName(task.AssignedUser) : null,
            AssigneeAvatarUrl = task.AssignedUser?.AvatarUrl,
            CreatorId = task.CreatorId,
            DueDate = task.DueDate,
            Priority = task.Priority,
            CommentCount = commentCount,
            Order = task.Order,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            Status = status
        };
    }
    
    private static ParticipantRoleKind? GetRoleForUser(Event eventEntity, Guid userId)
    {
        if (eventEntity.ResponsiblePersonId == userId)
            return ParticipantRoleKind.Organizer;

        return eventEntity.EventRoles.FirstOrDefault(r => r.UserId == userId)?.ParticipantRole;
    }
    
    private static BoardTaskCommentResponse ToCommentDto(BoardTaskComment comment)
    {
        return new BoardTaskCommentResponse
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            AuthorId = comment.AuthorId,
            AuthorName = $"{comment.Author?.LastName} {comment.Author?.FirstName}".Trim(),
            Text = comment.Text,
            CreatedAt = comment.CreatedAt
        };
    }
    
    private static BoardTaskHistory CreateHistory(Guid taskId, Guid userId, string action, string? fieldName, string? oldValue, string? newValue)
    {
        return new BoardTaskHistory
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ChangedByUserId = userId,
            Action = action,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue
        };
    }

    private static BoardTaskHistoryResponse ToHistoryDto(BoardTaskHistory history)
    {
        return new BoardTaskHistoryResponse
        {
            Id = history.Id,
            TaskId = history.TaskId,
            ChangedByUserId = history.ChangedByUserId,
            Action = history.Action,
            FieldName = history.FieldName,
            OldValue = history.OldValue,
            NewValue = history.NewValue,
            CreatedAt = history.CreatedAt
        };
    }

    private static BoardTasksResponse ToMyTaskDto(BoardTask t)
    {
        return new BoardTasksResponse
        {
            Id = t.Id,
            EventId = t.Column.EventId,
            EventName = t.Column.Event?.Name ?? string.Empty,
            EventAvatarUrl = t.Column.Event?.Avatar ?? string.Empty,
            ColumnId = t.ColumnId,
            Status = t.Column.Name,
            Title = t.Title,
            Description = t.Description,
            AssignedUserId = t.AssignedUserId,
            CreatorId = t.CreatorId,
            DueDate = t.DueDate,
            Priority = t.Priority,
            Order = t.Order,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}