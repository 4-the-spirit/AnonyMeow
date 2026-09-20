using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class UserEndpointsTests(PostgresContainerFixture postgresFixture)
{
    private CustomWebApplicationFactory CreateFactory() => new(postgresFixture.ConnectionString);

    private HttpClient CreateAuthenticatedClient(string oid)
    {
        var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(oid));
        return client;
    }

    [Fact]
    public async Task CompleteProfile_HappyPath_ReturnsOkWithUser()
    {
        var oid = Guid.NewGuid().ToString();
        using var client = CreateAuthenticatedClient(oid);

        var response = await client.PostAsJsonAsync("/api/auth/complete-profile", new CompleteProfileRequest(
            $"user{Guid.NewGuid():N}"[..12], "Display Name", "seed-1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>(TestJsonOptions.Default);
        Assert.NotNull(body);
        Assert.Equal("Display Name", body!.DisplayName);
    }

    [Fact]
    public async Task CompleteProfile_DuplicateUsername_ReturnsConflict()
    {
        var username = $"dup{Guid.NewGuid():N}"[..12];

        using var firstClient = CreateAuthenticatedClient(Guid.NewGuid().ToString());
        var firstResponse = await firstClient.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, "First User", "seed-1"));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var secondClient = CreateAuthenticatedClient(Guid.NewGuid().ToString());
        var secondResponse = await secondClient.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, "Second User", "seed-2"));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetMe_BeforeProfileCompletion_ReturnsForbidden()
    {
        using var client = CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_AfterProfileCompletion_ReturnsOwnProfile()
    {
        var oid = Guid.NewGuid().ToString();
        var username = $"me{Guid.NewGuid():N}"[..12];
        using var client = CreateAuthenticatedClient(oid);
        await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, "Me User", "seed-1"));

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>(TestJsonOptions.Default);
        Assert.Equal(username, body!.Username);
        // A freshly onboarded user is never a platform admin — that flag is only ever set directly in the DB.
        Assert.False(body.IsPlatformAdmin);
    }

    [Fact]
    public async Task GetByUsername_UnknownUser_ReturnsNotFound()
    {
        var oid = Guid.NewGuid().ToString();
        var username = $"known{Guid.NewGuid():N}"[..12];
        using var client = CreateAuthenticatedClient(oid);
        await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, "Known User", "seed-1"));

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid():N}nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserComments_EchoesPostIdAndAncestorChain_ForDeepLinking()
    {
        using var factory = CreateFactory();
        var oid = Guid.NewGuid().ToString();
        var username = $"deeplink{Guid.NewGuid():N}"[..15];
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(oid));
        await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, username, "seed"));

        var communityName = TestNames.UniqueCommunityName();
        await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairsResponse = await client.GetAsync($"/api/communities/{communityName}/flairs");
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        var createPost = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("A post", "Body", null, null, null, flairs![0].Id));
        var post = await createPost.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var createRoot = await client.PostAsJsonAsync(
            $"/api/posts/{post!.Id}/comments", new CreateCommentRequest("Root", null));
        var root = await createRoot.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);
        var createReply = await client.PostAsJsonAsync(
            $"/api/posts/{post.Id}/comments", new CreateCommentRequest("Reply", root!.Id));
        var reply = await createReply.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        var response = await client.GetAsync($"/api/users/{username}/comments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);

        var rootFromList = list!.Items.Single(c => c.Id == root.Id);
        Assert.Equal(post.Id, rootFromList.PostId);
        Assert.Empty(rootFromList.AncestorCommentIds ?? []);

        var replyFromList = list.Items.Single(c => c.Id == reply!.Id);
        Assert.Equal(post.Id, replyFromList.PostId);
        Assert.Equal([root.Id], replyFromList.AncestorCommentIds);
    }

    [Fact]
    public async Task GetByUsernamePostsAndComments_WithoutAuth_ReturnAnonymousReads()
    {
        using var factory = CreateFactory();
        var oid = Guid.NewGuid().ToString();
        var username = $"anonprofile{Guid.NewGuid():N}"[..15];
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(oid));
        await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, username, "seed"));

        var communityName = TestNames.UniqueCommunityName();
        await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairsResponse = await client.GetAsync($"/api/communities/{communityName}/flairs");
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("A post", "Body", null, null, null, flairs![0].Id));
        var post = await (await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Another post", "Body", null, null, null, flairs[0].Id))).Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        await client.PostAsJsonAsync($"/api/posts/{post!.Id}/comments", new CreateCommentRequest("A comment", null));

        using var anonymousClient = factory.CreateClient();

        var profileResponse = await anonymousClient.GetAsync($"/api/users/{username}");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var postsResponse = await anonymousClient.GetAsync($"/api/users/{username}/posts");
        Assert.Equal(HttpStatusCode.OK, postsResponse.StatusCode);

        var commentsResponse = await anonymousClient.GetAsync($"/api/users/{username}/comments");
        Assert.Equal(HttpStatusCode.OK, commentsResponse.StatusCode);
    }

    [Fact]
    public async Task GetUserCommunities_ReturnsJoinedCommunitiesWithRole()
    {
        using var factory = CreateFactory();
        var username = $"joinedc{Guid.NewGuid():N}"[..15];
        using var client = CreateAuthenticatedClient(Guid.NewGuid().ToString());
        await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, username, "seed"));

        var ownedCommunity = TestNames.UniqueCommunityName();
        await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(ownedCommunity, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        var joinedOnlyCommunity = TestNames.UniqueCommunityName();
        using var otherClient = CreateAuthenticatedClient(Guid.NewGuid().ToString());
        await otherClient.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest($"owner{Guid.NewGuid():N}"[..15], "Owner", "seed"));
        await otherClient.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(joinedOnlyCommunity, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        await client.PostAsync($"/api/communities/{joinedOnlyCommunity}/join", null);

        var response = await client.GetAsync($"/api/users/{username}/communities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<PagedResponse<CommunityMembershipResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, list!.TotalCount);
        Assert.Equal(CommunityRole.Moderator, list.Items.Single(c => c.Name == ownedCommunity).Role);
        Assert.Equal(CommunityRole.Member, list.Items.Single(c => c.Name == joinedOnlyCommunity).Role);
    }
}
