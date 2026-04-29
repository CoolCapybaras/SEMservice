using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class BoardTaskCommentRepository : IBoardTaskCommentRepository
{
    private readonly ApplicationDbContext _context;

    public BoardTaskCommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BoardTaskComment> AddAsync(BoardTaskComment comment)
    {
        _context.BoardTaskComments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task<List<BoardTaskComment>> GetByTaskIdAsync(Guid taskId)
    {
        return await _context.BoardTaskComments
            .Where(c => c.TaskId == taskId)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }
}