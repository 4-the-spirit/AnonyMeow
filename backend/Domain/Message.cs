namespace AnonyMeow.Domain;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public required string Body { get; set; }
    public bool IsRead { get; set; }
    public bool IsRemoved { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}
