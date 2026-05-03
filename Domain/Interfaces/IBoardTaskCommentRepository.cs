using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IBoardTaskCommentRepository
{
    Task<BoardTaskComment> AddAsync(BoardTaskComment comment);
    Task<List<BoardTaskComment>> GetByTaskIdAsync(Guid taskId);
    Task<Dictionary<Guid, int>> GetCommentCountsByTaskIdsAsync(IReadOnlyCollection<Guid> taskIds);
}