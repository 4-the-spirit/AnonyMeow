using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class PlatformRestriction
{
    public Guid Id { get; set; }
    public Guid AppUserId { get; set; }
    public PlatformRestrictionType Type { get; set; }
    public required string Reason { get; set; }
    public Guid IssuedByAdminId { get; set; }
    public DateTimeOffset StartAtUtc { get; set; }
    public DateTimeOffset? EndAtUtc { get; set; }
    public PlatformRestrictionStatus Status { get; set; }
}
