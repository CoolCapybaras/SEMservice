namespace SEM.Domain.Models;

public class Category
{
    public Guid id { get; set; }
    public string Name { get; set; }

    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}