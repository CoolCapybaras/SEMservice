using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IBoardTaskHistoryRepository
{
    Task AddAsync(BoardTaskHistory entry);
    Task AddRangeAsync(IEnumerable<BoardTaskHistory> entries);
    Task<List<BoardTaskHistory>> GetByTaskIdAsync(Guid taskId);
}