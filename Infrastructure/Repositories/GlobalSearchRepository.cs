using Domain.DTO;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public sealed class GlobalSearchRepository : IGlobalSearchRepository
{
    private readonly ApplicationDbContext _context;

    public GlobalSearchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GlobalSearchUserDto>> SearchUsersAsync(string q, int limit)
    {
        var pattern = $"%{q}%";

        return await _context.Users
            .Where(u =>
                (u.FirstName != null && EF.Functions.ILike(u.FirstName, pattern)) ||
                (u.LastName != null && EF.Functions.ILike(u.LastName, pattern)) ||
                (u.Profession != null && EF.Functions.ILike(u.Profession, pattern)) ||
                (u.City != null && EF.Functions.ILike(u.City, pattern)))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(limit)
            .Select(u => new GlobalSearchUserDto
            {
                Id = u.Id,
                LastName = u.LastName,
                FirstName = u.FirstName,
                Profession = u.Profession,
                City = u.City,
                AvatarUrl = u.AvatarUrl
            })
            .ToListAsync();
    }

    public async Task<List<GlobalSearchEventDto>> SearchEventsAsync(string q, int limit)
    {
        var pattern = $"%{q}%";

        return await _context.Events
            .Where(e => e.LifecycleState != EventLifecycleState.Archived)
            .Where(e => e.Name != null && EF.Functions.ILike(e.Name, pattern))
            .OrderBy(e => e.StartDate)
            .Take(limit)
            .Select(e => new GlobalSearchEventDto
            {
                Id = e.Id,
                Name = e.Name ?? string.Empty,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Auditorium = e.Auditorium,
                VenueFormat = e.VenueFormat,
                LifecycleState = e.LifecycleState,
                Color = e.Color,
                Avatar = e.Avatar
            })
            .ToListAsync();
    }

    public async Task<List<GlobalSearchEventDto>> SearchArchivedEventsAsync(string q, int limit)
    {
        var pattern = $"%{q}%";

        return await _context.Events
            .Where(e => e.LifecycleState == EventLifecycleState.Archived)
            .Where(e => e.Name != null && EF.Functions.ILike(e.Name, pattern))
            .OrderByDescending(e => e.StartDate)
            .Take(limit)
            .Select(e => new GlobalSearchEventDto
            {
                Id = e.Id,
                Name = e.Name ?? string.Empty,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Auditorium = e.Auditorium,
                VenueFormat = e.VenueFormat,
                LifecycleState = e.LifecycleState,
                Color = e.Color,
                Avatar = e.Avatar
            })
            .ToListAsync();
    }

    public async Task<List<RecentSearchQueryDto>> GetRecentQueriesAsync(Guid userId, string q, int limit)
    {
        var pattern = $"%{q}%";
        var query = _context.SearchQueryHistories
            .Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => EF.Functions.ILike(x.Query, pattern));

        return await query
            .OrderByDescending(x => x.LastUsedAt)
            .Take(limit)
            .Select(x => new RecentSearchQueryDto
            {
                Query = x.Query,
                LastUsedAt = x.LastUsedAt,
                UseCount = x.UseCount
            })
            .ToListAsync();
    }

    public async Task UpsertRecentQueryAsync(Guid userId, string query, string normalizedQuery, DateTime nowUtc)
    {
        var existing = await _context.SearchQueryHistories
            .FirstOrDefaultAsync(x => x.UserId == userId && x.NormalizedQuery == normalizedQuery);

        if (existing == null)
        {
            _context.SearchQueryHistories.Add(new SearchQueryHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Query = query,
                NormalizedQuery = normalizedQuery,
                LastUsedAt = nowUtc,
                UseCount = 1
            });
        }
        else
        {
            existing.Query = query;
            existing.LastUsedAt = nowUtc;
            existing.UseCount += 1;
            _context.SearchQueryHistories.Update(existing);
        }

        await _context.SaveChangesAsync();
    }
}

