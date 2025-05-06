namespace Domain.DTO;

public class SearchRequest
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string? Name { get; set; }
    public List<Guid>? Organizators { get; set; }
    public string? Format { get; set; }
    public bool? HasFreePlaces { get; set; }
    public List<string>? Categories { get; set; }
    public int Offset { get; set; }
    public int Count { get; set; }
}