using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class SavedCommentEndpointsTests(PostgresContainerFixture postgresFixture)
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

    private static async Task<(Guid PostId, Guid CommentId, string CommunityName)> CreateCommentAsync(HttpClient client)
    {
        var communityName = TestNames.UniqueCommunityName();
        var createCommunity = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        createCommunity.EnsureSuccessStatusCode();

        var flairsResponse = await client.GetAsync($"/api/communities/{communityName}/flairs");
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);

        var createPost = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("A post", "Body", null, null, null, flairs![0].Id));
        createPost.EnsureSuccessStatusCode();
        var post = await createPost.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var createComment = await client.PostAsJsonAsync($"/api/posts/{post!.Id}/comments", new CreateCommentRequest("A comment", null));
        createComment.EnsureSuccessStatusCode();
        var comment = await createComment.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        return (post.Id, comment!.Id, communityName);
    }

    [Fact]
    public async Task SaveAndUnsaveComment_HappyPath()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "csaver");
        var (postId, commentId, communityName) = await CreateCommentAsync(client);

        var saveResponse = await client.PostAsync($"/api/comments/{commentId}/save", null);
        Assert.Equal(HttpStatusCode.NoContent, saveResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/users/me/saved-comments");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        var savedComment = Assert.Single(page!.Items, c => c.Id == commentId);
        Assert.Equal(postId, savedComment.PostId);
        Assert.Equal(communityName, savedComment.CommunityName);
        Assert.NotNull(savedComment.AncestorCommentIds);
        Assert.Empty(savedComment.AncestorCommentIds!);

        var unsaveResponse = await client.DeleteAsync($"/api/comments/{commentId}/save");
        Assert.Equal(HttpStatusCode.NoContent, unsaveResponse.StatusCode);

        var listAfterUnsave = await (await client.GetAsync("/api/users/me/saved-comments"))
            .Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        Assert.DoesNotContain(listAfterUnsave!.Items, c => c.Id == commentId);
    }

    [Fact]
    public async Task SaveComment_NonExistentComment_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "csavenf");

        var response = await client.PostAsync($"/api/comments/{Guid.NewGuid()}/save", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SaveComment_IsPerUser_NotSharedAcrossViewers()
    {
        using var factory = CreateFactory();
        using var owner = await CreateClientWithCompletedProfileAsync(factory, "commentowner");
        using var other = await CreateClientWithCompletedProfileAsync(factory, "otherviewer2");
        var (_, commentId, _) = await CreateCommentAsync(owner);

        await owner.PostAsync($"/api/comments/{commentId}/save", null);

        var otherList = await (await other.GetAsync("/api/users/me/saved-comments"))
            .Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        Assert.Empty(otherList!.Items);
    }
}
