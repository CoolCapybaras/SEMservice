using SEM.Domain.Models;

namespace Domain.DTO;

public class EventLifecycleUpdateRequest
{
    public EventLifecycleState LifecycleState { get; set; }
}