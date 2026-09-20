using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Communities;

public record CommunityMemberResponse(
    string Username,
    string? DisplayName,
    string? AvatarSeed,
    CommunityRole Role,
    DateTimeOffset JoinedAtUtc);
