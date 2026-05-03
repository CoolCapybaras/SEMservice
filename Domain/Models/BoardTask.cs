using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SEM.Domain.Models;

public class BoardTask
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ColumnId { get; set; }

    [ForeignKey(nameof(ColumnId))]
    public BoardColumn Column { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? AssignedUserId { get; set; }

    public User? AssignedUser { get; set; }

    public Guid CreatorId { get; set; }
    
    public DateTime? DueDate { get; set; }

    public BoardTaskPriority Priority { get; set; } = BoardTaskPriority.Medium;

    public DateTime? DeadlineReminderSentAt { get; set; }

    public DateTime? OverdueNotificationSentAt { get; set; }

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<BoardTaskComment> Comments { get; set; } = new List<BoardTaskComment>();
}