namespace Domain.DTO;

public class UpdateChatMessageRequest
{
    public string? Text { get; set; }
    
    public List<Guid>? RemoveAttachmentIds { get; set; }
}