using Domain.DTO;

namespace SEM.Domain.Interfaces;

public interface IGlobalSearchRepository
{
    Task<List<GlobalSearchUserDto>> SearchUsersAsync(string q, int limit);
    Task<List<GlobalSearchEventDto>> SearchEventsAsync(string q, int limit);
    Task<List<GlobalSearchEventDto>> SearchArchivedEventsAsync(string q, int limit);

    Task<List<RecentSearchQueryDto>> GetRecentQueriesAsync(Guid userId, string q, int limit);
    Task UpsertRecentQueryAsync(Guid userId, string query, string normalizedQuery, DateTime nowUtc);
}