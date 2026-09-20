using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public bool IsRead { get; set; }
    public required string PreviewText { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
