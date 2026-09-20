using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class SavedPostAndFeedEndpointsTests(PostgresContainerFixture postgresFixture)
{
    private CustomWebApplicationFactory CreateFactory() => new(postgresFixture.ConnectionString);

    private static async Task<HttpClient> CreateClientWithCompletedProfileAsync(CustomWebApplicationFactory factory, string usernamePrefix)
    {
        var oid = Guid.NewGuid().ToString();
        var username = $"{usernamePrefix}{Guid.NewGuid():N}"[..15];
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(oid));

        var response = await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, username, "seed"));
        response.EnsureSuccessStatusCode();

        return client;
    }

    private static async Task<string> CreateCommunityAsync(HttpClient client)
    {
        var name = TestNames.UniqueCommunityName();
        var response = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        response.EnsureSuccessStatusCode();
        return name;
    }

    private static async Task<Guid> CreatePostAsync(HttpClient client, string communityName, string title = "A post")
    {
        var flairsResponse = await client.GetAsync($"/api/communities/{communityName}/flairs");
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);

        var response = await client.PostAsJsonAsync(
            $"/api/communities/{communityName}/posts", new CreatePostRequest(title, "Body", null, null, null, flairs![0].Id));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        return body!.Id;
    }

    [Fact]
    public async Task SaveAndUnsavePost_HappyPath()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "saver");
        var communityName = await CreateCommunityAsync(client);
        var postId = await CreatePostAsync(client, communityName);

        var saveResponse = await client.PostAsync($"/api/posts/{postId}/save", null);
        Assert.Equal(HttpStatusCode.NoContent, saveResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/users/me/saved-posts");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(page!.Items, p => p.Id == postId);

        var unsaveResponse = await client.DeleteAsync($"/api/posts/{postId}/save");
        Assert.Equal(HttpStatusCode.NoContent, unsaveResponse.StatusCode);

        var listAfterUnsave = await (await client.GetAsync("/api/users/me/saved-posts"))
            .Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.DoesNotContain(listAfterUnsave!.Items, p => p.Id == postId);
    }

    [Fact]
    public async Task SavePost_NonExistentPost_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "savenf");

        var response = await client.PostAsync($"/api/posts/{Guid.NewGuid()}/save", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SavePost_IsPerUser_NotSharedAcrossViewers()
    {
        using var factory = CreateFactory();
        using var owner = await CreateClientWithCompletedProfileAsync(factory, "feedowner");
        using var other = await CreateClientWithCompletedProfileAsync(factory, "otherviewer");
        var communityName = await CreateCommunityAsync(owner);
        var postId = await CreatePostAsync(owner, communityName);

        await owner.PostAsync($"/api/posts/{postId}/save", null);

        var otherList = await (await other.GetAsync("/api/users/me/saved-posts"))
            .Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Empty(otherList!.Items);
    }

    [Fact]
    public async Task Feed_ReturnsPosts_OnlyFromJoinedCommunities()
    {
        using var factory = CreateFactory();
        using var a = await CreateClientWithCompletedProfileAsync(factory, "feeda");
        using var b = await CreateClientWithCompletedProfileAsync(factory, "feedb");
        var communityA = await CreateCommunityAsync(a);
        var communityB = await CreateCommunityAsync(b);
        var postA = await CreatePostAsync(a, communityA, "Post in A");
        var postB = await CreatePostAsync(b, communityB, "Post in B");

        var aFeed = await (await a.GetAsync("/api/feed")).Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(aFeed!.Items, p => p.Id == postA);
        Assert.DoesNotContain(aFeed.Items, p => p.Id == postB);

        // Once B joins A's community, A's post enters B's feed too — the creator of a community
        // is auto-joined as a moderator (CommunityService.CreateAsync), but a second user has to
        // join explicitly.
        var joinResponse = await b.PostAsync($"/api/communities/{communityA}/join", null);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        var bFeedAfterJoin = await (await b.GetAsync("/api/feed"))
            .Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(bFeedAfterJoin!.Items, p => p.Id == postA);
    }

    [Fact]
    public async Task Feed_NoJoinedCommunities_ReturnsEmpty()
    {
        using var factory = CreateFactory();
        using var lonely = await CreateClientWithCompletedProfileAsync(factory, "lonelyuser");

        var feed = await (await lonely.GetAsync("/api/feed")).Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);

        Assert.Empty(feed!.Items);
    }

    [Fact]
    public async Task Feed_AnonymousCaller_ReturnsPostsFromEveryCommunity()
    {
        using var factory = CreateFactory();
        using var a = await CreateClientWithCompletedProfileAsync(factory, "anonfeeda");
        using var b = await CreateClientWithCompletedProfileAsync(factory, "anonfeedb");
        var communityA = await CreateCommunityAsync(a);
        var communityB = await CreateCommunityAsync(b);
        var postA = await CreatePostAsync(a, communityA, "Post in A");
        var postB = await CreatePostAsync(b, communityB, "Post in B");

        using var anonymous = factory.CreateClient();
        var response = await anonymous.GetAsync("/api/feed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var feed = await response.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(feed!.Items, p => p.Id == postA);
        Assert.Contains(feed.Items, p => p.Id == postB);
    }
}
