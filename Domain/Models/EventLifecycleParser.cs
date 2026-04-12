namespace SEM.Domain.Models;

public static class EventLifecycleParser
{
    public static EventLifecycleState Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return EventLifecycleState.Published;
        return raw.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" or "PUBLISHED" or "ОПУБЛИКОВАНО" => EventLifecycleState.Published,
            "FINISHED" or "COMPLETED" or "ЗАВЕРШЕНО" => EventLifecycleState.Completed,
            "CANCELLED" or "ОТМЕНЕНО" => EventLifecycleState.Cancelled,
            "DRAFT" or "ЧЕРНОВИК" => EventLifecycleState.Draft,
            _ => EventLifecycleState.Published
        };
    }
}