using Domain;
using Domain.DTO;
using Domain.Interfaces;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class BoardService: IBoardService
{
    private readonly IBoardTaskRepository _taskRepository;
    private readonly IBoardColumnRepository _columnRepository;
    private readonly IEventRepository _eventRepository;

    public BoardService(IBoardTaskRepository taskRepository, IBoardColumnRepository columnRepository, IEventRepository eventRepository)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _eventRepository = eventRepository;
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

        return ServiceResult<BoardTask>.Ok(task);
    }
}