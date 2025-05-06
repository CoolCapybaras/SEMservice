namespace SEM.Domain.Models;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public ICollection<EventCategory> EventCategories { get; set; }
}