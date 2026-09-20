using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Admin;

public record CreatePlatformRestrictionRequest(PlatformRestrictionType Type, string Reason, DateTimeOffset? EndAtUtc);
