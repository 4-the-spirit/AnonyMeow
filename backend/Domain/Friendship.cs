using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class Friendship
{
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public FriendshipStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; }
}
