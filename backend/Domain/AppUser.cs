using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class AppUser
{
    public Guid Id { get; set; }
    public required string B2CObjectId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarSeed { get; set; }
    public int Karma { get; set; }
    public FriendListVisibility FriendListVisibility { get; set; } = FriendListVisibility.Everyone;
    public bool IsPlatformAdmin { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
}
