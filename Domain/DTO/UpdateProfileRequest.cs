using SEM.Domain.Models;

namespace Domain.DTO;

public class UpdateProfileRequest
{
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Profession { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Telegram { get; set; }
    public string? City { get; set; }
    public UiTheme? Theme { get; set; }
    public NotificationChannel? NotificationChannel { get; set; }
    public bool? NotifyTaskAssigned { get; set; }
    public bool? NotifyTaskDeadline { get; set; }
    public bool? NotifyEventStart { get; set; }
    public bool? NotifyEventCancelled { get; set; }
}