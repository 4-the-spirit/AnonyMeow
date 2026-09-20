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
public class ProfileEndpointsTests(PostgresContainerFixture postgresFixture)
{
    private CustomWebApplicationFactory CreateFactory() => new(postgresFixture.ConnectionString);

    private static async Task<HttpClient> CreateClientWithCompletedProfileAsync(CustomWebApplicationFactory factory, string username)
    {
        var oid = Guid.NewGuid().ToString();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(oid));

        var response = await client.PostAsJsonAsync("/api/auth/complete-profile",
            new CompleteProfileRequest(username, username, "seed"));
        response.EnsureSuccessStatusCode();

        return client;
    }

    [Fact]
    public async Task GetUserPosts_ExcludesRemovedPost_ForNonAuthorViewer_ButIncludesForAuthor()
    {
        using var factory = CreateFactory();
        var authorUsername = $"profauthor{Guid.NewGuid():N}"[..15];
        using var author = await CreateClientWithCompletedProfileAsync(factory, authorUsername);
        using var viewer = await CreateClientWithCompletedProfileAsync(factory, $"profviewer{Guid.NewGuid():N}"[..15]);

        var communityName = TestNames.UniqueCommunityName();
        await author.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairsResponse1 = await author.GetAsync($"/api/communities/{communityName}/flairs");
        var flairs1 = await flairsResponse1.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);

        var keptResponse = await author.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Kept post", "Body", null, null, null, flairs1![0].Id));
        var kept = await keptResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var removedResponse = await author.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Removed post", "Body", null, null, null, flairs1[0].Id));
        var removed = await removedResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        await author.DeleteAsync($"/api/posts/{removed!.Id}");

        var viewerListResponse = await viewer.GetAsync($"/api/users/{authorUsername}/posts");
        var viewerList = await viewerListResponse.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Single(viewerList!.Items);
        Assert.Equal(kept!.Id, viewerList.Items[0].Id);

        var authorListResponse = await author.GetAsync($"/api/users/{authorUsername}/posts");
        var authorList = await authorListResponse.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, authorList!.Items.Count);
    }

    [Fact]
    public async Task GetUserComments_ExcludesRemovedComment_ForNonAuthorViewer_ButIncludesForAuthor()
    {
        using var factory = CreateFactory();
        var authorUsername = $"cprofauthor{Guid.NewGuid():N}"[..15];
        using var author = await CreateClientWithCompletedProfileAsync(factory, authorUsername);
        using var viewer = await CreateClientWithCompletedProfileAsync(factory, $"cprofviewer{Guid.NewGuid():N}"[..15]);

        var communityName = TestNames.UniqueCommunityName();
        await author.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairsResponse2 = await author.GetAsync($"/api/communities/{communityName}/flairs");
        var flairs2 = await flairsResponse2.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        var postResponse = await author.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Post", "Body", null, null, null, flairs2![0].Id));
        var post = await postResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var keptResponse = await author.PostAsJsonAsync(
            $"/api/posts/{post!.Id}/comments", new CreateCommentRequest("Kept comment", null));
        var kept = await keptResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        var removedResponse = await author.PostAsJsonAsync(
            $"/api/posts/{post.Id}/comments", new CreateCommentRequest("Removed comment", null));
        var removed = await removedResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);
        await author.DeleteAsync($"/api/comments/{removed!.Id}");

        var viewerListResponse = await viewer.GetAsync($"/api/users/{authorUsername}/comments");
        var viewerList = await viewerListResponse.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        Assert.Single(viewerList!.Items);
        Assert.Equal(kept!.Id, viewerList.Items[0].Id);

        var authorListResponse = await author.GetAsync($"/api/users/{authorUsername}/comments");
        var authorList = await authorListResponse.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, authorList!.Items.Count);
    }
}
