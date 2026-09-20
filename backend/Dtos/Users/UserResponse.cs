using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Users;

public record UserResponse(
    string Username,
    string? DisplayName,
    string? AvatarSeed,
    int Karma,
    FriendListVisibility FriendListVisibility,
    DateTimeOffset CreatedAtUtc,
    bool IsPlatformAdmin)
{
    public static UserResponse FromEntity(AppUser user) => new(
        user.Username ?? string.Empty,
        user.DisplayName,
        user.AvatarSeed,
        user.Karma,
        user.FriendListVisibility,
        user.CreatedAtUtc,
        user.IsPlatformAdmin);
}
