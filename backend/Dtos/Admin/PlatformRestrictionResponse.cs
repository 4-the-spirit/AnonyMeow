using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Admin;

public record PlatformRestrictionResponse(
    Guid Id, string Username, PlatformRestrictionType Type, string Reason, DateTimeOffset StartAtUtc,
    DateTimeOffset? EndAtUtc, PlatformRestrictionStatus Status)
{
    public static PlatformRestrictionResponse FromEntity(PlatformRestriction restriction, string username) =>
        new(restriction.Id, username, restriction.Type, restriction.Reason, restriction.StartAtUtc,
            restriction.EndAtUtc, restriction.Status);
}
