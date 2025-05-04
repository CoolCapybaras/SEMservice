namespace Domain.DTO;

public class SearchRequest
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string? Name { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Organizators { get; set; } = new();
    public string? Format { get; set; }
    public bool? isFreePlaces { get; set; }
}