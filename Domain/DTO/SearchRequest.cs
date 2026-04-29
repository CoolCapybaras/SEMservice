using SEM.Domain.Models;

namespace Domain.DTO;

public class SearchRequest
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string? Name { get; set; }
    public List<Guid>? Organizators { get; set; }

    /// <summary>Фильтр по формату (новый контракт).</summary>
    public VenueFormat? VenueFormat { get; set; }

    /// <summary>Устаревший строковый формат; используется, если <see cref="VenueFormat"/> не задан.</summary>
    public string? Format { get; set; }
    public bool? HasFreePlaces { get; set; }
    public List<string>? Categories { get; set; }
    public List<EventTypeKind>? Types { get; set; }
    public int Offset { get; set; }
    public int Count { get; set; }
}