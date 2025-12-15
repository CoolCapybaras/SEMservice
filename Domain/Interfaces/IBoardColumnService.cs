using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IBoardColumnService
{
    Task<ServiceResult<BoardColumn>> CreateColumnAsync(Guid eventId, string name, Guid userId);

    Task<ServiceResult<List<BoardColumn>>> GetColumnsAsync(Guid eventId);

    Task<ServiceResult<BoardColumn?>> GetColumnByIdAsync(Guid columnId);

    Task<ServiceResult<BoardColumn>> UpdateColumnAsync(Guid columnId, BoardColumnUpdateRequest request, Guid userId);

    Task<ServiceResult<bool>> DeleteColumnAsync(Guid columnId, Guid userId);
}