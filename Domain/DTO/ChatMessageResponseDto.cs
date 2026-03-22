namespace Domain.DTO;

public class ChatAttachmentDto
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class ChatSenderDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
}

public class ChatReplyPreviewDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public ChatSenderDto Sender { get; set; } = new();
}

public class ChatMessageResponseDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public ChatReplyPreviewDto? ReplyTo { get; set; }
    public ChatSenderDto Sender { get; set; } = new();
    public List<ChatAttachmentDto> Attachments { get; set; } = new();
}