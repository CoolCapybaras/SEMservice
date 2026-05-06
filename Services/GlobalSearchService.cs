using Domain;
using Domain.DTO;
using SEM.Domain.Interfaces;

namespace SEM.Services;

public sealed class GlobalSearchService : IGlobalSearchService
{
    private readonly IGlobalSearchRepository _repo;

    public GlobalSearchService(IGlobalSearchRepository repo)
    {
        _repo = repo;
    }

    public async Task<ServiceResult<GlobalSearchResponse>> SearchAsync(GlobalSearchRequest request, Guid userId)
    {
        var q = (request.Q ?? string.Empty).Trim();
        var normalized = Normalize(q);

        var usersLimit = Clamp(request.UsersLimit, 0, 50);
        var eventsLimit = Clamp(request.EventsLimit, 0, 50);
        var archivedLimit = Clamp(request.ArchivedEventsLimit, 0, 50);
        var recentLimit = Clamp(request.RecentLimit, 0, 50);

        if (!string.IsNullOrWhiteSpace(q))
            await _repo.UpsertRecentQueryAsync(userId, q, normalized, DateTime.UtcNow);

        var response = new GlobalSearchResponse { Query = q };

        if (!string.IsNullOrWhiteSpace(q))
        {
            // Важно: все запросы используют один scoped DbContext через репозиторий,
            // поэтому выполняем их последовательно (иначе будет "A second operation was started on this context").
            response.Users = await _repo.SearchUsersAsync(q, usersLimit);
            response.Events = await _repo.SearchEventsAsync(q, eventsLimit);
            response.ArchivedEvents = await _repo.SearchArchivedEventsAsync(q, archivedLimit);
            response.RecentQueries = await _repo.GetRecentQueriesAsync(userId, q, recentLimit);
        }
        else
        {
            response.RecentQueries = await _repo.GetRecentQueriesAsync(userId, q, recentLimit);
        }

        return ServiceResult<GlobalSearchResponse>.Ok(response);
    }

    private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Нормализация нужна для upsert истории: trim + lower + схлопывание пробелов.
        var s = input.Trim().ToLowerInvariant();
        while (s.Contains("  "))
            s = s.Replace("  ", " ");
        return s;
    }
}

