using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IBoardColumnRepository
{
    Task<BoardColumn> AddColumnAsync(BoardColumn column);
    Task<List<BoardColumn>> GetColumnsAsync(Guid eventId);
    Task<BoardColumn?> GetColumnByIdAsync(Guid columnId);
    Task UpdateColumnAsync(BoardColumn column);
    Task DeleteColumnAsync(BoardColumn column);
}