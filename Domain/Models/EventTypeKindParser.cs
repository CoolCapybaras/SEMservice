namespace SEM.Domain.Models;

public static class EventTypeKindParser
{
    public static bool TryParse(string? raw, out EventTypeKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var s = raw.Trim();
        if (Enum.TryParse<EventTypeKind>(s, true, out kind))
            return true;
        var lower = s.ToLowerInvariant();
        if (lower.Contains("хакатон") || lower.Contains("hackathon")) { kind = EventTypeKind.Hackathon; return true; }
        if (lower.Contains("лекци") || lower.Contains("lecture")) { kind = EventTypeKind.Lecture; return true; }
        if (lower == "пп" || lower == "pp") { kind = EventTypeKind.PP; return true; }
        if (lower.Contains("спецкурс") || lower.Contains("special")) { kind = EventTypeKind.SpecialCourse; return true; }
        if (lower.Contains("практик") || lower.Contains("practice")) { kind = EventTypeKind.Practice; return true; }
        if (lower.Contains("карьерное меропроиятие") || lower.Contains("CareerEvent")) { kind = EventTypeKind.CareerEvent; return true; }
        return false;
    }
}