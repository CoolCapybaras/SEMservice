using SEM.Domain.Models;

namespace Domain.DTO;

public class EventRequest
{
    public Event newEvent { get; set; }
    public List<Category> newCategorys { get; set; }
}