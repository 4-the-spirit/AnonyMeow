using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class FlairAndReactionEndpointsTests(PostgresContainerFixture postgresFixture)
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

    private static async Task<string> CreateCommunityAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        response.EnsureSuccessStatusCode();
        return name;
    }

    private static async Task<Guid> GetFirstFlairIdAsync(HttpClient client, string communityName)
    {
        var response = await client.GetAsync($"/api/communities/{communityName}/flairs");
        response.EnsureSuccessStatusCode();
        var flairs = await response.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        return flairs![0].Id;
    }

    [Fact]
    public async Task CreateFlair_ModOnly_AndRejectsDuplicateName()
    {
        using var factory = CreateFactory();
        using var mod = await CreateClientWithCompletedProfileAsync(factory, $"flairmod{Guid.NewGuid():N}"[..15]);
        using var other = await CreateClientWithCompletedProfileAsync(factory, $"flairother{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(mod, TestNames.UniqueCommunityName());

        var forbidden = await other.PostAsJsonAsync(
            $"/api/communities/{communityName}/flairs", new CreateFlairRequest("Discussion", "#ff0000"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var created = await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/flairs", new CreateFlairRequest("Discussion", "#ff0000"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var duplicate = await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/flairs", new CreateFlairRequest("Discussion", "#00ff00"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var listResponse = await mod.GetAsync($"/api/communities/{communityName}/flairs");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var flairs = await listResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        Assert.Contains(flairs!, f => f.Name == "Discussion");

        using var anonymousClient = factory.CreateClient();
        var anonymousListResponse = await anonymousClient.GetAsync($"/api/communities/{communityName}/flairs");
        Assert.Equal(HttpStatusCode.OK, anonymousListResponse.StatusCode);
    }

    [Fact]
    public async Task AssignFlair_HappyPath_AndRejectsCrossCommunityFlair()
    {
        using var factory = CreateFactory();
        using var author = await CreateClientWithCompletedProfileAsync(factory, $"flairauthor{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(author, TestNames.UniqueCommunityName());
        var otherCommunityName = await CreateCommunityAsync(author, TestNames.UniqueCommunityName());

        var flairResponse = await author.PostAsJsonAsync(
            $"/api/communities/{communityName}/flairs", new CreateFlairRequest("Discussion", "#ff0000"));
        var flair = await flairResponse.Content.ReadFromJsonAsync<FlairResponse>(TestJsonOptions.Default);

        var otherFlairResponse = await author.PostAsJsonAsync(
            $"/api/communities/{otherCommunityName}/flairs", new CreateFlairRequest("OffTopic", "#0000ff"));
        var otherFlair = await otherFlairResponse.Content.ReadFromJsonAsync<FlairResponse>(TestJsonOptions.Default);

        var postResponse = await author.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Flaired post", "Body", null, null, null, flair!.Id));
        var post = await postResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        // Cross-community flair is rejected.
        var mismatch = await author.PatchAsJsonAsync(
            $"/api/posts/{post!.Id}/flair", new UpdatePostFlairRequest(otherFlair!.Id));
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);

        // Same-community flair succeeds.
        var assigned = await author.PatchAsJsonAsync($"/api/posts/{post.Id}/flair", new UpdatePostFlairRequest(flair!.Id));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var assignedBody = await assigned.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        Assert.Equal(flair.Id, assignedBody!.Flair!.Id);

        // Deleting the flair (mod-only, same user here) clears it from the post.
        var deleteResponse = await author.DeleteAsync($"/api/communities/{communityName}/flairs/{flair.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var afterDelete = await author.GetAsync($"/api/posts/{post.Id}");
        var afterDeleteBody = await afterDelete.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        Assert.Null(afterDeleteBody!.Flair);
    }

    [Fact]
    public async Task UpdateFlair_ModOnly_EditsCustomFlair_ButRejectsDefaultFlair()
    {
        using var factory = CreateFactory();
        using var mod = await CreateClientWithCompletedProfileAsync(factory, $"flaireditmod{Guid.NewGuid():N}"[..15]);
        using var other = await CreateClientWithCompletedProfileAsync(factory, $"flaireditother{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(mod, TestNames.UniqueCommunityName());

        var createdResponse = await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/flairs", new CreateFlairRequest("Discussion", "#ff0000"));
        var customFlair = await createdResponse.Content.ReadFromJsonAsync<FlairResponse>(TestJsonOptions.Default);

        var forbidden = await other.PatchAsJsonAsync(
            $"/api/communities/{communityName}/flairs/{customFlair!.Id}", new UpdateFlairRequest("Renamed", "#00ff00"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var updated = await mod.PatchAsJsonAsync(
            $"/api/communities/{communityName}/flairs/{customFlair.Id}", new UpdateFlairRequest("Renamed", "#00ff00"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await updated.Content.ReadFromJsonAsync<FlairResponse>(TestJsonOptions.Default);
        Assert.Equal("Renamed", updatedBody!.Name);
        Assert.Equal("#00ff00", updatedBody.ColorHex);
        Assert.False(updatedBody.IsDefault);

        var flairsResponse = await mod.GetAsync($"/api/communities/{communityName}/flairs");
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        var defaultFlair = flairs!.First(f => f.IsDefault);

        var rejectedUpdate = await mod.PatchAsJsonAsync(
            $"/api/communities/{communityName}/flairs/{defaultFlair.Id}", new UpdateFlairRequest("New Name", "#ffffff"));
        Assert.Equal(HttpStatusCode.BadRequest, rejectedUpdate.StatusCode);

        var rejectedDelete = await mod.DeleteAsync($"/api/communities/{communityName}/flairs/{defaultFlair.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, rejectedDelete.StatusCode);
    }

    [Fact]
    public async Task PostReactions_AddAndRemove_AggregateCorrectly()
    {
        using var factory = CreateFactory();
        using var author = await CreateClientWithCompletedProfileAsync(factory, $"reactauthor{Guid.NewGuid():N}"[..15]);
        using var reactor = await CreateClientWithCompletedProfileAsync(factory, $"reactor{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(author, TestNames.UniqueCommunityName());
        var flairId = await GetFirstFlairIdAsync(author, communityName);
        var postResponse = await author.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Reactable post", "Body", null, null, null, flairId));
        var post = await postResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var invalidEmoji = await reactor.PutAsync($"/api/posts/{post!.Id}/reactions/{Uri.EscapeDataString("🦄")}", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidEmoji.StatusCode);

        var addResponse = await reactor.PutAsync($"/api/posts/{post.Id}/reactions/{Uri.EscapeDataString("🔥")}", null);
        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        var afterAdd = await reactor.GetAsync($"/api/posts/{post.Id}");
        var afterAddBody = await afterAdd.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        var fireReaction = afterAddBody!.Reactions!.Single(r => r.Emoji == "🔥");
        Assert.Equal(1, fireReaction.Count);
        Assert.True(fireReaction.ReactedByViewer);

        var removeResponse = await reactor.DeleteAsync($"/api/posts/{post.Id}/reactions/{Uri.EscapeDataString("🔥")}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var afterRemove = await reactor.GetAsync($"/api/posts/{post.Id}");
        var afterRemoveBody = await afterRemove.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        Assert.Empty(afterRemoveBody!.Reactions!);
    }

    [Fact]
    public async Task CommentReactions_AddAndRemove_AggregateCorrectly()
    {
        using var factory = CreateFactory();
        using var author = await CreateClientWithCompletedProfileAsync(factory, $"creactauthor{Guid.NewGuid():N}"[..15]);
        using var reactor = await CreateClientWithCompletedProfileAsync(factory, $"creactor{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(author, TestNames.UniqueCommunityName());
        var flairId = await GetFirstFlairIdAsync(author, communityName);
        var postResponse = await author.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Post for comment reactions", "Body", null, null, null, flairId));
        var post = await postResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        var commentResponse = await author.PostAsJsonAsync(
            $"/api/posts/{post!.Id}/comments", new CreateCommentRequest("A comment", null));
        var comment = await commentResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        var invalidEmoji = await reactor.PutAsync($"/api/comments/{comment!.Id}/reactions/{Uri.EscapeDataString("🦄")}", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidEmoji.StatusCode);

        var addResponse = await reactor.PutAsync($"/api/comments/{comment.Id}/reactions/{Uri.EscapeDataString("❤️")}", null);
        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        var afterAdd = await reactor.GetAsync($"/api/posts/{post.Id}/comments");
        var page = await afterAdd.Content.ReadFromJsonAsync<AnonyMeow.Dtos.Common.PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        var heartReaction = page!.Items.Single(c => c.Id == comment.Id).Reactions!.Single(r => r.Emoji == "❤️");
        Assert.Equal(1, heartReaction.Count);
        Assert.True(heartReaction.ReactedByViewer);

        var removeResponse = await reactor.DeleteAsync($"/api/comments/{comment.Id}/reactions/{Uri.EscapeDataString("❤️")}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var afterRemove = await reactor.GetAsync($"/api/posts/{post.Id}/comments");
        var pageAfterRemove = await afterRemove.Content.ReadFromJsonAsync<AnonyMeow.Dtos.Common.PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        Assert.Empty(pageAfterRemove!.Items.Single(c => c.Id == comment.Id).Reactions!);
    }
}
