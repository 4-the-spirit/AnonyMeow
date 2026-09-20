using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Communities;

public record CommunityMembershipResponse(
    string Name,
    string? IconImageUrl,
    CommunityRole Role,
    DateTimeOffset JoinedAtUtc);
