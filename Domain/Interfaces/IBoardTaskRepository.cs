using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IBoardTaskRepository
{
    Task<BoardTask> AddTaskAsync(BoardTask task);
    Task<List<BoardTask>> GetTasksByColumnIdAsync(Guid columnId);
    Task<BoardTask?> GetTaskByIdAsync(Guid taskId);
    Task UpdateTaskAsync(BoardTask task);
    Task DeleteTaskAsync(BoardTask task);

    Task MoveTasksAsync(
        List<BoardTask> oldColumnTasks,
        List<BoardTask> newColumnTasks,
        BoardTask movedTask);
    
    Task<List<BoardTask>> GetCurrentUserTasksAsync(Guid userId);
    Task<List<BoardTask>> GetCurrentUserTasksByEventAsync(Guid userId, Guid eventId);
}