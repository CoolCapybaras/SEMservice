using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IBoardService
{
    Task<ServiceResult<List<BoardDto>>> GetBoardAsync(Guid eventId, Guid userId, BoardKanbanQuery? query = null);
    Task<ServiceResult<BoardKanbanFacetsResponse>> GetBoardFacetsAsync(Guid eventId, Guid userId);
    Task<ServiceResult<BoardTask>> MoveTaskAsync(Guid taskId, MoveTaskRequest request, Guid userId);
}