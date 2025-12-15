using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IBoardTaskService
{
    Task<ServiceResult<BoardTaskDto>> CreateTaskAsync(Guid columnId, string title, string? description, Guid? assigneeId, DateTime? deadline, Guid userId);
    Task<ServiceResult<List<BoardTaskDto>>> GetTasksAsync(Guid columnId);
    Task<ServiceResult<BoardTaskDto?>> GetTaskByIdAsync(Guid taskId);
    Task<ServiceResult<BoardTaskDto>> UpdateTaskAsync(Guid taskId, BoardTaskUpdateRequest request, Guid userId);
    Task<ServiceResult<bool>> DeleteTaskAsync(Guid taskId, Guid userId);
}