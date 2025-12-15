using Domain;
using Domain.DTO;
using Domain.Interfaces;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class BoardTaskService: IBoardTaskService
{
    private readonly IBoardTaskRepository _repository;
    private readonly IBoardColumnRepository _columnRepository;
    private readonly IEventRepository _eventRepository;

    public BoardTaskService(IBoardTaskRepository repository, IBoardColumnRepository columnRepository,  IEventRepository eventRepository)
    {
        _repository = repository;
        _columnRepository = columnRepository;
        _eventRepository = eventRepository;
    }

    public async Task<ServiceResult<BoardTaskDto>> CreateTaskAsync(Guid columnId, string title, string? description, Guid? assigneeId, DateTime? deadline,
        Guid userId)
    {
        var column = await _columnRepository.GetColumnByIdAsync(columnId);
        if (column == null)
            return ServiceResult<BoardTaskDto>.Fail("Столбец не найден");

        var eventEntity = await _eventRepository.GetEventByIdAsync(column.EventId);
        if (eventEntity.ResponsiblePersonId != userId)
            return ServiceResult<BoardTaskDto>.Fail("Вы не можете добавлять задачи в этот столбец");
        
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
            Order = maxOrder + 1
        };

        await _repository.AddTaskAsync(task);
        return ServiceResult<BoardTaskDto>.Ok(ToDto(task, column.Name));
    }

    public async Task<ServiceResult<List<BoardTaskDto>>> GetTasksAsync(Guid columnId)
    {
        var tasks = await _repository.GetTasksByColumnIdAsync(columnId);
        var column = await _columnRepository.GetColumnByIdAsync(columnId);
        var dtoList = tasks.Select(t => ToDto(t, column.Name)).ToList();
        return ServiceResult<List<BoardTaskDto>>.Ok(dtoList);
    }

    public async Task<ServiceResult<BoardTaskDto?>> GetTaskByIdAsync(Guid taskId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        var column = await _columnRepository.GetColumnByIdAsync(task.ColumnId);
        return ServiceResult<BoardTaskDto?>.Ok(ToDto(task, column.Name));
    }

    public async Task<ServiceResult<BoardTaskDto>> UpdateTaskAsync(Guid taskId, BoardTaskUpdateRequest request, Guid userId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<BoardTaskDto>.Fail("Задача не найдена");

        var column = await _columnRepository.GetColumnByIdAsync(task.ColumnId);
        var eventEntity = await _eventRepository.GetEventByIdAsync(column.EventId);

        if (eventEntity.ResponsiblePersonId != userId && task.CreatorId != userId)
            return ServiceResult<BoardTaskDto>.Fail("Вы не можете изменять эту задачу");

        // Обновляем поля
        task.Title = request.Title ?? task.Title;
        task.Description = request.Description ?? task.Description;
        task.AssignedUserId = request.AssigneeId ?? task.AssignedUserId;
        task.DueDate = request.Deadline ?? task.DueDate;

        task.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateTaskAsync(task);

        return ServiceResult<BoardTaskDto>.Ok(ToDto(task, column.Name));;
    }

    public async Task<ServiceResult<bool>> DeleteTaskAsync(Guid taskId, Guid userId)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
            return ServiceResult<bool>.Fail("Задача не найдена");

        var column = await _columnRepository.GetColumnByIdAsync(task.ColumnId);
        var eventEntity = await _eventRepository.GetEventByIdAsync(column.EventId);

        if (eventEntity.ResponsiblePersonId != userId && task.CreatorId != userId)
            return ServiceResult<bool>.Fail("Вы не можете удалять эту задачу");

        await _repository.DeleteTaskAsync(task);
        return ServiceResult<bool>.Ok(true);
    }
    
    private BoardTaskDto ToDto(BoardTask task, string status)
    {
        return new BoardTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            AssignedUserId = task.AssignedUserId,
            CreatorId = task.CreatorId,
            DueDate = task.DueDate,
            Order = task.Order,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            Status = status
        };
    }
}