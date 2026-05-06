namespace Domain.DTO;

public sealed class GlobalSearchRequest
{
    /// <summary>Текст поиска. Ищем по вхождению (ILIKE %q%).</summary>
    public string? Q { get; set; }

    public int UsersLimit { get; set; } = 10;
    public int EventsLimit { get; set; } = 10;
    public int ArchivedEventsLimit { get; set; } = 10;
    public int RecentLimit { get; set; } = 10;
}