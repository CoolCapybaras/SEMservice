using System.Globalization;
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
    private readonly IBoardTaskCommentRepository _commentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IHubContext<BoardHub> _boardHubContext;

    public BoardService(
        IBoardTaskRepository taskRepository,
        IBoardTaskHistoryRepository historyRepository,
        IBoardColumnRepository columnRepository,
        IBoardTaskCommentRepository commentRepository,
        IEventRepository eventRepository,
        IHubContext<BoardHub> boardHubContext)
    {
        _taskRepository = taskRepository;
        _historyRepository = historyRepository;
        _columnRepository = columnRepository;
        _commentRepository = commentRepository;
        _eventRepository = eventRepository;
        _boardHubContext = boardHubContext;
    }

    public async Task<ServiceResult<List<BoardDto>>> GetBoardAsync(Guid eventId, Guid userId, BoardKanbanQuery? query)
    {
        var eventEntity = await _eventRepository.GetEventByIdAsync(eventId);
        if (eventEntity == null)
            return ServiceResult<List<BoardDto>>.Fail("Мероприятие не найдено");
        if (!IsParticipant(eventEntity, userId))
            return ServiceResult<List<BoardDto>>.Fail("Вы не являетесь участником мероприятия");

        var columns = await _columnRepository.GetColumnsAsync(eventId);
        var allTaskIds = columns.SelectMany(c => c.Tasks).Select(t => t.Id).ToList();
        var commentCounts = await _commentRepository.GetCommentCountsByTaskIdsAsync(allTaskIds);
        var q = query ?? new BoardKanbanQuery();
        var utcNow = DateTime.UtcNow;

        var boardDto = new BoardDto
        {
            EventId = eventId,
            Columns = columns.OrderBy(c => c.Order).Select(c =>
            {
                var tasks = c.Tasks.AsEnumerable();
                tasks = ApplyKanbanFilters(tasks, q, userId, utcNow);
                var sorted = SortKanbanTasks(tasks, q.Sort ?? "UrgentFirst");

                return new BoardColumnDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Order = c.Order,
                    Tasks = sorted.Select((t, index) => MapBoardTaskDto(t, c.Name, commentCounts, index)).ToList()
                };
            }).ToList()
        };

        return ServiceResult<List<BoardDto>>.Ok(new List<BoardDto> { boardDto });
    }

    public async Task<ServiceResult<BoardKanbanFacetsResponse>> GetBoardFacetsAsync(Guid eventId, Guid userId)
    {
        var eventEntity = await _eventRepository.GetEventByIdAsync(eventId);
        if (eventEntity == null)
            return ServiceResult<BoardKanbanFacetsResponse>.Fail("Мероприятие не найдено");
        if (!IsParticipant(eventEntity, userId))
            return ServiceResult<BoardKanbanFacetsResponse>.Fail("Вы не являетесь участником мероприятия");

        var columns = await _columnRepository.GetColumnsAsync(eventId);
        var assignees = columns
            .SelectMany(c => c.Tasks)
            .Where(t => t.AssignedUserId.HasValue && t.AssignedUser != null)
            .Select(t => t.AssignedUser!)
            .GroupBy(u => u.Id)
            .Select(g => g.First())
            .OrderBy(u => UserDisplayName(u), StringComparer.Create(new CultureInfo("ru-RU"), ignoreCase: true))
            .Select(u => new BoardKanbanAssigneeFacet
            {
                Id = u.Id,
                DisplayName = UserDisplayName(u),
                AvatarUrl = u.AvatarUrl
            })
            .ToList();

        return ServiceResult<BoardKanbanFacetsResponse>.Ok(new BoardKanbanFacetsResponse { Assignees = assignees });
    }

    private static BoardTaskDto MapBoardTaskDto(BoardTask t, string status, IReadOnlyDictionary<Guid, int> commentCounts, int visualOrder)
    {
        commentCounts.TryGetValue(t.Id, out var cc);
        return new BoardTaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            AssignedUserId = t.AssignedUserId,
            AssigneeDisplayName = t.AssignedUser != null ? UserDisplayName(t.AssignedUser) : null,
            AssigneeAvatarUrl = t.AssignedUser?.AvatarUrl,
            CreatorId = t.CreatorId,
            DueDate = t.DueDate,
            Priority = t.Priority,
            CommentCount = cc,
            Order = visualOrder,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            Status = status
        };
    }

    private static IEnumerable<BoardTask> ApplyKanbanFilters(IEnumerable<BoardTask> tasks, BoardKanbanQuery q, Guid userId, DateTime utcNow)
    {
        var seq = tasks;

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var needle = q.Q.Trim();
            seq = seq.Where(t =>
                t.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (t.Description?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (q.MineOnly)
            seq = seq.Where(t => t.AssignedUserId == userId);

        var assigneeIds = ParseGuidSet(q.AssigneeIds);
        if (assigneeIds.Count > 0)
            seq = seq.Where(t => t.AssignedUserId.HasValue && assigneeIds.Contains(t.AssignedUserId.Value));

        var priorities = ParsePrioritySet(q.Priorities);
        if (priorities.Count > 0)
            seq = seq.Where(t => priorities.Contains(t.Priority));

        var deadlineTokens = ParseTokens(q.Deadlines);
        if (deadlineTokens.Count > 0)
        {
            seq = seq.Where(t =>
            {
                if (!t.DueDate.HasValue)
                    return false;
                return deadlineTokens.Any(token => MatchesDeadlineBucket(t.DueDate, token, utcNow));
            });
        }

        return seq;
    }

    private static List<BoardTask> SortKanbanTasks(IEnumerable<BoardTask> tasks, string sort)
    {
        var list = tasks.ToList();
        var mode = (sort ?? "UrgentFirst").Trim();
        switch (mode.ToLowerInvariant())
        {
            case "newest":
                list.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
                break;
            case "oldest":
                list.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
                break;
            case "assigneeasc":
                list.Sort((a, b) =>
                {
                    var an = a.AssignedUser != null ? UserDisplayName(a.AssignedUser) : "\uFFFF";
                    var bn = b.AssignedUser != null ? UserDisplayName(b.AssignedUser) : "\uFFFF";
                    var c = string.Compare(an, bn, StringComparison.CurrentCultureIgnoreCase);
                    return c != 0 ? c : string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase);
                });
                break;
            default:
                list.Sort((a, b) =>
                {
                    var p = a.Priority.CompareTo(b.Priority);
                    if (p != 0) return p;
                    var da = a.DueDate ?? DateTime.MaxValue;
                    var db = b.DueDate ?? DateTime.MaxValue;
                    var d = da.CompareTo(db);
                    return d != 0 ? d : a.Order.CompareTo(b.Order);
                });
                break;
        }

        return list;
    }

    private static HashSet<Guid> ParseGuidSet(string? raw)
    {
        var set = new HashSet<Guid>();
        if (string.IsNullOrWhiteSpace(raw))
            return set;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var id))
                set.Add(id);
        }

        return set;
    }

    private static HashSet<string> ParseTokens(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return set;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            set.Add(part);
        return set;
    }

    private static HashSet<BoardTaskPriority> ParsePrioritySet(string? raw)
    {
        var set = new HashSet<BoardTaskPriority>();
        if (string.IsNullOrWhiteSpace(raw))
            return set;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<BoardTaskPriority>(part, ignoreCase: true, out var p))
                set.Add(p);
        }

        return set;
    }

    private static bool MatchesDeadlineBucket(DateTime? dueRaw, string token, DateTime utcNow)
    {
        if (!dueRaw.HasValue)
            return false;
        var due = dueRaw.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dueRaw.Value, DateTimeKind.Utc)
            : dueRaw.Value.ToUniversalTime();
        var day = due.Date;
        var today = utcNow.Date;
        switch (token.ToLowerInvariant())
        {
            case "overdue":
            case "просрочен":
                return day < today;
            case "today":
            case "сегодня":
                return day == today;
            case "tomorrow":
            case "завтра":
                return day == today.AddDays(1);
            case "thisweek":
            case "наэтойнеделе":
            {
                var diff = (today.DayOfWeek - DayOfWeek.Monday + 7) % 7;
                var weekStart = today.AddDays(-diff);
                var weekEnd = weekStart.AddDays(6);
                return day >= weekStart && day <= weekEnd;
            }
            default:
                return false;
        }
    }

    private static string UserDisplayName(User u)
    {
        var parts = new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim());
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? (u.Email ?? u.Id.ToString()) : name;
    }

    private static bool IsParticipant(Event evt, Guid userId) =>
        userId == evt.ResponsiblePersonId || evt.EventRoles.Any(r => r.UserId == userId);

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