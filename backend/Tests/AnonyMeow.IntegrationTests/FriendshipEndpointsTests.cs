using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Friends;
using AnonyMeow.Dtos.Notifications;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class FriendshipEndpointsTests(PostgresContainerFixture postgresFixture)
{
    private CustomWebApplicationFactory CreateFactory() => new(postgresFixture.ConnectionString);

    private static async Task<(HttpClient Client, string Username)> CreateClientWithCompletedProfileAsync(
        CustomWebApplicationFactory factory, string usernamePrefix)
    {
        var oid = Guid.NewGuid().ToString();
        var username = $"{usernamePrefix}{Guid.NewGuid():N}"[..15];
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(oid));

        var response = await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, username, "seed"));
        response.EnsureSuccessStatusCode();

        return (client, username);
    }

    [Fact]
    public async Task RequestAcceptAndList_HappyPath()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "frienda");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "friendb");

        var requestResponse = await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        var request = await requestResponse.Content.ReadFromJsonAsync<FriendRequestResponse>(TestJsonOptions.Default);
        Assert.Equal(FriendshipStatusText.Pending, request!.Status.ToString());

        var acceptResponse = await b.PostAsync($"/api/users/{aUsername}/friend-requests/accept", null);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var aFriendsResponse = await a.GetAsync($"/api/users/{aUsername}/friends");
        Assert.Equal(HttpStatusCode.OK, aFriendsResponse.StatusCode);
        var aFriends = await aFriendsResponse.Content.ReadFromJsonAsync<List<FriendResponse>>(TestJsonOptions.Default);
        Assert.Contains(aFriends!, f => f.Username == bUsername);

        var bFriendsResponse = await b.GetAsync($"/api/users/{bUsername}/friends");
        var bFriends = await bFriendsResponse.Content.ReadFromJsonAsync<List<FriendResponse>>(TestJsonOptions.Default);
        Assert.Contains(bFriends!, f => f.Username == aUsername);

        var unfriendResponse = await a.DeleteAsync($"/api/friends/{bUsername}");
        Assert.Equal(HttpStatusCode.NoContent, unfriendResponse.StatusCode);

        var aFriendsAfterRemove = await a.GetAsync($"/api/users/{aUsername}/friends");
        var aFriendsAfterRemoveBody = await aFriendsAfterRemove.Content.ReadFromJsonAsync<List<FriendResponse>>(TestJsonOptions.Default);
        Assert.DoesNotContain(aFriendsAfterRemoveBody!, f => f.Username == bUsername);
    }

    [Fact]
    public async Task RequestAsync_DuplicateRequest_ReturnsConflict()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "dupfrienda");
        var (_, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "dupfriendb");

        var first = await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RequestAsync_SelfRequest_ReturnsBadRequest()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "selffriend");

        var response = await a.PostAsync($"/api/users/{aUsername}/friend-requests", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeclineRequest_LeavesUsersUnfriended_AndAllowsRetry()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "declinea");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "declineb");

        await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);
        var declineResponse = await b.PostAsync($"/api/users/{aUsername}/friend-requests/decline", null);
        Assert.Equal(HttpStatusCode.OK, declineResponse.StatusCode);

        var areFriends = await a.GetAsync($"/api/users/{aUsername}/friends");
        var friends = await areFriends.Content.ReadFromJsonAsync<List<FriendResponse>>(TestJsonOptions.Default);
        Assert.DoesNotContain(friends!, f => f.Username == bUsername);

        // A fresh request after a decline is allowed (not blocked as a duplicate).
        var retryResponse = await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);
        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
    }

    [Fact]
    public async Task ListFriends_RespectsNoOneVisibility()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "visa");
        var (b, _) = await CreateClientWithCompletedProfileAsync(factory, "visb");

        var patchResponse = await a.PatchAsJsonAsync(
            "/api/users/me", new UpdateProfileRequest(null, null, FriendListVisibility.NoOne));
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Owner can still see their own list.
        var ownResponse = await a.GetAsync($"/api/users/{aUsername}/friends");
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);

        // A different viewer is forbidden.
        var otherResponse = await b.GetAsync($"/api/users/{aUsername}/friends");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task ListFriends_RespectsFriendsOnlyVisibility()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "foa");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "fob");
        var (c, _) = await CreateClientWithCompletedProfileAsync(factory, "foc");

        await a.PatchAsJsonAsync("/api/users/me", new UpdateProfileRequest(null, null, FriendListVisibility.FriendsOnly));

        await b.PostAsync($"/api/users/{aUsername}/friend-requests", null);
        await a.PostAsync($"/api/users/{bUsername}/friend-requests/accept", null);

        // A non-friend is forbidden.
        var nonFriendResponse = await c.GetAsync($"/api/users/{aUsername}/friends");
        Assert.Equal(HttpStatusCode.Forbidden, nonFriendResponse.StatusCode);

        // The accepted friend can view it.
        var friendResponse = await b.GetAsync($"/api/users/{aUsername}/friends");
        Assert.Equal(HttpStatusCode.OK, friendResponse.StatusCode);
    }

    [Fact]
    public async Task CancelRequest_WithdrawsOutgoingRequest_AndAllowsResend()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "cancela");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "cancelb");

        await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);

        var cancelResponse = await a.DeleteAsync($"/api/users/{bUsername}/friend-requests");
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var aRequestsAfterCancel = await (await a.GetAsync("/api/users/me/friend-requests"))
            .Content.ReadFromJsonAsync<FriendRequestsResponse>(TestJsonOptions.Default);
        Assert.DoesNotContain(aRequestsAfterCancel!.Outgoing, r => r.AddresseeUsername == bUsername);

        // Withdrawing frees the pair up for a fresh request, same as a decline would.
        var retryResponse = await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);
        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);

        // b never accepted/declined, so the addressee cannot "cancel" a's request either.
        var wrongDirectionCancel = await b.DeleteAsync($"/api/users/{aUsername}/friend-requests");
        Assert.Equal(HttpStatusCode.NoContent, wrongDirectionCancel.StatusCode);
        var aRequestsStillPending = await (await a.GetAsync("/api/users/me/friend-requests"))
            .Content.ReadFromJsonAsync<FriendRequestsResponse>(TestJsonOptions.Default);
        Assert.Contains(aRequestsStillPending!.Outgoing, r => r.AddresseeUsername == bUsername);
    }

    [Fact]
    public async Task ListMyFriendRequests_ReturnsIncomingAndOutgoing()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "reqa");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "reqb");
        var (c, cUsername) = await CreateClientWithCompletedProfileAsync(factory, "reqc");

        // a -> b: outgoing for a, incoming for b.
        await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);
        // c -> a: incoming for a, outgoing for c.
        await c.PostAsync($"/api/users/{aUsername}/friend-requests", null);

        var aRequestsResponse = await a.GetAsync("/api/users/me/friend-requests");
        Assert.Equal(HttpStatusCode.OK, aRequestsResponse.StatusCode);
        var aRequests = await aRequestsResponse.Content.ReadFromJsonAsync<FriendRequestsResponse>(TestJsonOptions.Default);
        Assert.Contains(aRequests!.Incoming, r => r.RequesterUsername == cUsername);
        Assert.Contains(aRequests.Outgoing, r => r.AddresseeUsername == bUsername);

        var bRequestsResponse = await b.GetAsync("/api/users/me/friend-requests");
        var bRequests = await bRequestsResponse.Content.ReadFromJsonAsync<FriendRequestsResponse>(TestJsonOptions.Default);
        Assert.Contains(bRequests!.Incoming, r => r.RequesterUsername == aUsername);
        Assert.Empty(bRequests.Outgoing);

        // Accepting clears the pending request from both lists.
        await b.PostAsync($"/api/users/{aUsername}/friend-requests/accept", null);
        var aRequestsAfterAccept = await (await a.GetAsync("/api/users/me/friend-requests"))
            .Content.ReadFromJsonAsync<FriendRequestsResponse>(TestJsonOptions.Default);
        Assert.DoesNotContain(aRequestsAfterAccept!.Outgoing, r => r.AddresseeUsername == bUsername);
    }

    [Fact]
    public async Task SendFriendRequest_ProducesFriendRequestNotification_ForAddressee()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "notifyfrienda");
        var (b, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "notifyfriendb");

        var requestResponse = await a.PostAsync($"/api/users/{bUsername}/friend-requests", null);
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);

        var bNotificationsResponse = await b.GetAsync("/api/notifications");
        var bNotifications = await bNotificationsResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        Assert.Contains(bNotifications!.Items, n => n.Type == NotificationType.FriendRequest);
    }
}

file static class FriendshipStatusText
{
    public const string Pending = "Pending";
}
