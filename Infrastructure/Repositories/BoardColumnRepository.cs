using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class BoardColumnRepository : IBoardColumnRepository
{
    private readonly ApplicationDbContext _context;

    public BoardColumnRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BoardColumn> AddColumnAsync(BoardColumn column)
    {
        await _context.BoardColumn.AddAsync(column);
        await _context.SaveChangesAsync();
        return column;
    }

    public async Task<List<BoardColumn>> GetColumnsAsync(Guid eventId)
    {
        return await _context.BoardColumn
            .Where(c => c.EventId == eventId)
            .Include(c => c.Tasks)
            .OrderBy(c => c.Order)
            .ToListAsync();
    }

    public async Task<BoardColumn?> GetColumnByIdAsync(Guid columnId)
    {
        return await _context.BoardColumn.FindAsync(columnId);
    }

    public async Task UpdateColumnAsync(BoardColumn column)
    {
        _context.BoardColumn.Update(column);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteColumnAsync(BoardColumn column)
    {
        _context.BoardColumn.Remove(column);
        await _context.SaveChangesAsync();
    }
}