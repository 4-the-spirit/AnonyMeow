using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class CommunityMembership
{
    public Guid CommunityId { get; set; }
    public Guid AppUserId { get; set; }
    public CommunityRole Role { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; }
}
