using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IBoardService
{
    Task<ServiceResult<List<BoardDto>>> GetBoardAsync(Guid eventId);
    Task<ServiceResult<BoardTask>> MoveTaskAsync(Guid taskId, MoveTaskRequest request, Guid userId);
}