namespace SEM.Domain.Models;

public static class VenueFormatParser
{
    public static VenueFormat Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return VenueFormat.InPerson;
        var s = raw.Trim().ToLowerInvariant();
        if (s.Contains("онлайн") || s == "online")
            return VenueFormat.Online;
        if (s.Contains("гибрид") || s == "hybrid")
            return VenueFormat.Hybrid;
        if (s.Contains("очно") || s.Contains("in-person") || s == "offline")
            return VenueFormat.InPerson;
        if (Enum.TryParse<VenueFormat>(raw, true, out var v))
            return v;
        return VenueFormat.InPerson;
    }
}