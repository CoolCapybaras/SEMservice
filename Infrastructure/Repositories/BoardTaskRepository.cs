using Domain.DTO;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class BoardTaskRepository: IBoardTaskRepository
{
    private readonly ApplicationDbContext _context;

    public BoardTaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BoardTask> AddTaskAsync(BoardTask task)
    {
        _context.BoardTasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<List<BoardTask>> GetTasksByColumnIdAsync(Guid columnId)
    {
        return await _context.BoardTasks
            .Where(t => t.ColumnId == columnId)
            .Include(t => t.Column)
            .OrderBy(t => t.Order)
            .ToListAsync();
    }

    public async Task<BoardTask?> GetTaskByIdAsync(Guid taskId)
    {
        return await _context.BoardTasks
            .Include(t => t.Column)
            .FirstOrDefaultAsync(t => t.Id == taskId);
    }

    public async Task UpdateTaskAsync(BoardTask task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        _context.BoardTasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(BoardTask task)
    {
        _context.BoardTasks.Remove(task);
        await _context.SaveChangesAsync();
    }
    
    public async Task MoveTasksAsync(
        List<BoardTask> oldColumnTasks,
        List<BoardTask> newColumnTasks,
        BoardTask movedTask)
    {
        foreach (var t in oldColumnTasks)
            _context.BoardTasks.Update(t);

        foreach (var t in newColumnTasks)
            _context.BoardTasks.Update(t);

        _context.BoardTasks.Update(movedTask);

        await _context.SaveChangesAsync();
    }
    
    public async Task<List<BoardTask>> GetCurrentUserTasksAsync(Guid userId)
    {
        return await _context.BoardTasks
            .Where(t => t.AssignedUserId == userId)
            .Include(t => t.Column)
            .ThenInclude(c => c.Event)
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<List<BoardTask>> GetCurrentUserTasksByEventAsync(Guid userId, Guid eventId)
    {
        return await _context.BoardTasks
            .Where(t => t.AssignedUserId == userId && t.Column.EventId == eventId)
            .Include(t => t.Column)
            .ThenInclude(c => c.Event)
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public Task<List<BoardTasksResponse>> GetCurrentUserTasksAsync(Guid userId, Guid eventId)
    {
        throw new NotImplementedException();
    }
}