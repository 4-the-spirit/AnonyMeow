using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Friends;

public record FriendRequestResponse(
    string RequesterUsername, string? RequesterDisplayName,
    string AddresseeUsername, string? AddresseeDisplayName,
    FriendshipStatus Status, DateTimeOffset CreatedAtUtc)
{
    public static FriendRequestResponse FromEntity(
        Friendship friendship, string requesterUsername, string addresseeUsername,
        string? requesterDisplayName = null, string? addresseeDisplayName = null) => new(
        requesterUsername, requesterDisplayName, addresseeUsername, addresseeDisplayName,
        friendship.Status, friendship.CreatedAtUtc);
}
