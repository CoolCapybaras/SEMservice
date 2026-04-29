using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class BoardTaskHistoryRepository : IBoardTaskHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public BoardTaskHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(BoardTaskHistory entry)
    {
        _context.BoardTaskHistories.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<BoardTaskHistory> entries)
    {
        _context.BoardTaskHistories.AddRange(entries);
        await _context.SaveChangesAsync();
    }

    public async Task<List<BoardTaskHistory>> GetByTaskIdAsync(Guid taskId)
    {
        return await _context.BoardTaskHistories
            .Where(h => h.TaskId == taskId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }
}