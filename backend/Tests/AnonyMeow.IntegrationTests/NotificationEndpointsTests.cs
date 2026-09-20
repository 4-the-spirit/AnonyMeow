using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Moderation;
using AnonyMeow.Dtos.Notifications;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class NotificationEndpointsTests(PostgresContainerFixture postgresFixture)
{
    private CustomWebApplicationFactory CreateFactory() => new(postgresFixture.ConnectionString);

    private static async Task<(HttpClient Client, string Username)> CreateClientWithCompletedProfileAsync(
        CustomWebApplicationFactory factory, string usernamePrefix)
    {
        var oid = Guid.NewGuid().ToString();
        var shortPrefix = usernamePrefix.Length > 12 ? usernamePrefix[..12] : usernamePrefix;
        var username = $"{shortPrefix}{Guid.NewGuid():N}"[..(shortPrefix.Length + 8)];
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(oid));

        var response = await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, username, "seed"));
        response.EnsureSuccessStatusCode();

        return (client, username);
    }

    [Fact]
    public async Task BanningAUser_ProducesRetrievableModActionNotification_ThatCanBeMarkedRead()
    {
        using var factory = CreateFactory();
        var (mod, _) = await CreateClientWithCompletedProfileAsync(factory, "notifmod");
        var (target, targetUsername) = await CreateClientWithCompletedProfileAsync(factory, "notiftarget");
        var communityName = TestNames.UniqueCommunityName();
        await mod.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        var banResponse = await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/mod/bans", new CreateBanRequest(targetUsername, "harassment"));
        Assert.Equal(HttpStatusCode.Created, banResponse.StatusCode);

        var listResponse = await target.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        var notification = Assert.Single(page!.Items);
        Assert.Equal(NotificationType.ModAction, notification.Type);
        Assert.False(notification.IsRead);

        var unreadOnlyResponse = await target.GetAsync("/api/notifications?unreadOnly=true");
        var unreadPage = await unreadOnlyResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        Assert.Single(unreadPage!.Items);

        var markReadResponse = await target.PostAsync($"/api/notifications/{notification.Id}/read", null);
        Assert.Equal(HttpStatusCode.OK, markReadResponse.StatusCode);

        var afterReadResponse = await target.GetAsync("/api/notifications?unreadOnly=true");
        var afterReadPage = await afterReadResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        Assert.Empty(afterReadPage!.Items);
    }

    [Fact]
    public async Task MarkRead_OnAnotherUsersNotification_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (mod, _) = await CreateClientWithCompletedProfileAsync(factory, "ownmod");
        var (target, targetUsername) = await CreateClientWithCompletedProfileAsync(factory, "owntarget");
        var (intruder, _) = await CreateClientWithCompletedProfileAsync(factory, "intruder");
        var communityName = TestNames.UniqueCommunityName();
        await mod.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/mod/bans", new CreateBanRequest(targetUsername, "harassment"));

        var listResponse = await target.GetAsync("/api/notifications");
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        var notification = Assert.Single(page!.Items);

        var intruderMarkReadResponse = await intruder.PostAsync($"/api/notifications/{notification.Id}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, intruderMarkReadResponse.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_MarksAllOfCurrentUsersNotificationsRead()
    {
        using var factory = CreateFactory();
        var (mod, _) = await CreateClientWithCompletedProfileAsync(factory, "allreadmod");
        var (target, targetUsername) = await CreateClientWithCompletedProfileAsync(factory, "allreadtarget");
        var communityName = TestNames.UniqueCommunityName();
        await mod.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/mod/bans", new CreateBanRequest(targetUsername, "spam"));
        await mod.DeleteAsync($"/api/communities/{communityName}/mod/bans/{targetUsername}");

        var beforeResponse = await target.GetAsync("/api/notifications?unreadOnly=true");
        var beforePage = await beforeResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, beforePage!.TotalCount);

        var markAllResponse = await target.PostAsync("/api/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, markAllResponse.StatusCode);

        var afterResponse = await target.GetAsync("/api/notifications?unreadOnly=true");
        var afterPage = await afterResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        Assert.Empty(afterPage!.Items);
    }
}
