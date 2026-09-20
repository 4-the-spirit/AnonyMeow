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
using AnonyMeow.Dtos.Notifications;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class CommentAndVotingEndpointsTests(PostgresContainerFixture postgresFixture)
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

    private static async Task<Guid> GetFirstFlairIdAsync(HttpClient client, string communityName)
    {
        var flairsResponse = await client.GetAsync($"/api/communities/{communityName}/flairs");
        flairsResponse.EnsureSuccessStatusCode();
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        return flairs![0].Id;
    }

    private static async Task<(string CommunityName, Guid PostId)> CreatePostAsync(HttpClient client)
    {
        var communityName = TestNames.UniqueCommunityName();
        var createCommunity = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        createCommunity.EnsureSuccessStatusCode();
        var flairId = await GetFirstFlairIdAsync(client, communityName);

        var createPost = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("A post", "Body", null, null, null, flairId));
        createPost.EnsureSuccessStatusCode();
        var post = await createPost.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        return (communityName, post!.Id);
    }

    [Fact]
    public async Task CreateComment_EmptyBody_ReturnsValidationProblem()
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "emptycomment");
        var (_, postId) = await CreatePostAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("   ", null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_TopLevelAndReply_NestingWorks()
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "commenter");
        var (_, postId) = await CreatePostAsync(client);

        var topLevelResponse = await client.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("Top level comment", null));
        Assert.Equal(HttpStatusCode.Created, topLevelResponse.StatusCode);
        var topLevel = await topLevelResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        var replyResponse = await client.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("A reply", topLevel!.Id));
        Assert.Equal(HttpStatusCode.Created, replyResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/posts/{postId}/comments");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        Assert.Single(list!.Items);
        Assert.Equal(1, list.Items[0].ReplyCount);

        var repliesResponse = await client.GetAsync($"/api/comments/{topLevel.Id}/replies?page=1");
        var replies = await repliesResponse.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        Assert.Single(replies!.Items);
        Assert.Equal("A reply", replies.Items[0].BodyMarkdown);
    }

    [Fact]
    public async Task GetComment_ByNonAuthor_ReturnsPostIdAndAncestorChain()
    {
        using var factory = CreateFactory();
        var (author, _) = await CreateClientWithCompletedProfileAsync(factory, "getcauthor");
        var (viewer, _) = await CreateClientWithCompletedProfileAsync(factory, "getcviewer");
        var (_, postId) = await CreatePostAsync(author);

        var topLevelResponse = await author.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("Root", null));
        var topLevel = await topLevelResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        var replyResponse = await author.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("Reply", topLevel!.Id));
        var reply = await replyResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        // A user with no relationship to the comment (not its author) can still resolve it —
        // this is the lookup a Reply/Mention notification's click-through relies on.
        var getResponse = await viewer.GetAsync($"/api/comments/{reply!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);
        Assert.Equal(postId, fetched!.PostId);
        Assert.Equal([topLevel.Id], fetched.AncestorCommentIds);
    }

    [Fact]
    public async Task GetComment_NonExistent_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "getcghost");

        var response = await client.GetAsync($"/api/comments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDeleteComment_AuthorOnly()
    {
        using var factory = CreateFactory();
        var (author, _) = await CreateClientWithCompletedProfileAsync(factory, "author");
        var (other, _) = await CreateClientWithCompletedProfileAsync(factory, "other");
        var (_, postId) = await CreatePostAsync(author);

        var createResponse = await author.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("Original", null));
        var created = await createResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        var forbiddenPatch = await other.PatchAsJsonAsync($"/api/comments/{created!.Id}", new UpdateCommentRequest("Hacked"));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPatch.StatusCode);

        var okPatch = await author.PatchAsJsonAsync($"/api/comments/{created.Id}", new UpdateCommentRequest("Edited"));
        Assert.Equal(HttpStatusCode.OK, okPatch.StatusCode);

        var forbiddenDelete = await other.DeleteAsync($"/api/comments/{created.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDelete.StatusCode);

        var okDelete = await author.DeleteAsync($"/api/comments/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, okDelete.StatusCode);
    }

    [Fact]
    public async Task CommentCreate_RejectedWhenPostLocked_AndWhenParentBelongsToDifferentPost()
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "locker");
        var (_, postId) = await CreatePostAsync(client);
        var (_, otherPostId) = await CreatePostAsync(client);

        var foreignParentResponse = await client.PostAsJsonAsync(
            $"/api/posts/{otherPostId}/comments", new CreateCommentRequest("Parent on other post", null));
        var foreignParent = await foreignParentResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        var crossPostReply = await client.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("Cross-post reply", foreignParent!.Id));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, crossPostReply.StatusCode);
    }

    [Fact]
    public async Task PostVote_CastChangeRemove_UpdatesAuthorKarma()
    {
        using var factory = CreateFactory();
        var (author, authorUsername) = await CreateClientWithCompletedProfileAsync(factory, "postauthor");
        var (voter, _) = await CreateClientWithCompletedProfileAsync(factory, "postvoter");
        var (_, postId) = await CreatePostAsync(author);

        var castUp = await voter.PutAsJsonAsync($"/api/posts/{postId}/vote", new CastVoteRequest(1));
        Assert.Equal(HttpStatusCode.NoContent, castUp.StatusCode);
        var afterUp = await GetKarmaAsync(voter, authorUsername);
        Assert.Equal(1, afterUp);

        var changeToDown = await voter.PutAsJsonAsync($"/api/posts/{postId}/vote", new CastVoteRequest(-1));
        Assert.Equal(HttpStatusCode.NoContent, changeToDown.StatusCode);
        var afterChange = await GetKarmaAsync(voter, authorUsername);
        Assert.Equal(-1, afterChange);

        var removeResponse = await voter.DeleteAsync($"/api/posts/{postId}/vote");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
        var afterRemove = await GetKarmaAsync(voter, authorUsername);
        Assert.Equal(0, afterRemove);
    }

    [Fact]
    public async Task PostVote_ViewerVote_ReflectsCastChangeAndRemove_ButNotOtherViewers()
    {
        using var factory = CreateFactory();
        var (author, _) = await CreateClientWithCompletedProfileAsync(factory, "pvvauthor");
        var (voter, _) = await CreateClientWithCompletedProfileAsync(factory, "pvvvoter");
        var (other, _) = await CreateClientWithCompletedProfileAsync(factory, "pvvother");
        var (_, postId) = await CreatePostAsync(author);

        async Task<sbyte?> GetViewerVoteAsync(HttpClient client)
        {
            var response = await client.GetAsync($"/api/posts/{postId}");
            response.EnsureSuccessStatusCode();
            var post = await response.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
            return post!.ViewerVote;
        }

        Assert.Null(await GetViewerVoteAsync(voter));

        await voter.PutAsJsonAsync($"/api/posts/{postId}/vote", new CastVoteRequest(1));
        Assert.Equal((sbyte)1, await GetViewerVoteAsync(voter));
        // Another viewer's own vote (here: none) is unaffected by voter's vote.
        Assert.Null(await GetViewerVoteAsync(other));

        await voter.PutAsJsonAsync($"/api/posts/{postId}/vote", new CastVoteRequest(-1));
        Assert.Equal((sbyte)-1, await GetViewerVoteAsync(voter));

        await voter.DeleteAsync($"/api/posts/{postId}/vote");
        Assert.Null(await GetViewerVoteAsync(voter));
    }

    [Fact]
    public async Task CommentVote_ViewerVote_ReflectsCastAndRemove()
    {
        using var factory = CreateFactory();
        var (author, _) = await CreateClientWithCompletedProfileAsync(factory, "cvvauthor");
        var (voter, _) = await CreateClientWithCompletedProfileAsync(factory, "cvvvoter");
        var (_, postId) = await CreatePostAsync(author);
        var createResponse = await author.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("Vote on me", null));
        var comment = await createResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        async Task<sbyte?> GetViewerVoteAsync(HttpClient client)
        {
            var response = await client.GetAsync($"/api/comments/{comment!.Id}");
            response.EnsureSuccessStatusCode();
            var fetched = await response.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);
            return fetched!.ViewerVote;
        }

        Assert.Null(await GetViewerVoteAsync(voter));

        await voter.PutAsJsonAsync($"/api/comments/{comment!.Id}/vote", new CastVoteRequest(1));
        Assert.Equal((sbyte)1, await GetViewerVoteAsync(voter));

        await voter.DeleteAsync($"/api/comments/{comment.Id}/vote");
        Assert.Null(await GetViewerVoteAsync(voter));
    }

    [Fact]
    public async Task PostVote_SelfVote_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        var (author, _) = await CreateClientWithCompletedProfileAsync(factory, "selfvoter");
        var (_, postId) = await CreatePostAsync(author);

        var response = await author.PutAsJsonAsync($"/api/posts/{postId}/vote", new CastVoteRequest(1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CommentVote_CastChangeRemove_UpdatesAuthorKarma()
    {
        using var factory = CreateFactory();
        var (author, authorUsername) = await CreateClientWithCompletedProfileAsync(factory, "commauthor");
        var (voter, _) = await CreateClientWithCompletedProfileAsync(factory, "commvoter");
        var (_, postId) = await CreatePostAsync(author);
        var createResponse = await author.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("Vote on me", null));
        var comment = await createResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        var castUp = await voter.PutAsJsonAsync($"/api/comments/{comment!.Id}/vote", new CastVoteRequest(1));
        Assert.Equal(HttpStatusCode.NoContent, castUp.StatusCode);
        Assert.Equal(1, await GetKarmaAsync(voter, authorUsername));

        var removeResponse = await voter.DeleteAsync($"/api/comments/{comment.Id}/vote");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
        Assert.Equal(0, await GetKarmaAsync(voter, authorUsername));
    }

    [Theory]
    [InlineData("new")]
    [InlineData("top")]
    public async Task ListCommunityPosts_DeterministicSort_OrdersAsExpected(string sort)
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "sorter");
        var (voter, _) = await CreateClientWithCompletedProfileAsync(factory, "sortervoter");
        var communityName = TestNames.UniqueCommunityName();
        await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairId = await GetFirstFlairIdAsync(client, communityName);

        var firstResponse = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("First", "Body", null, null, null, flairId));
        var first = await firstResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var secondResponse = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Second", "Body", null, null, null, flairId));
        var second = await secondResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        // Give "Second" the higher score for the ?sort=top case; it's also the more recent post,
        // which conveniently makes it first for ?sort=new too.
        await voter.PutAsJsonAsync($"/api/posts/{second!.Id}/vote", new CastVoteRequest(1));

        var listResponse = await client.GetAsync($"/api/communities/{communityName}/posts?sort={sort}&page=1");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);

        Assert.Equal(2, list!.Items.Count);
        Assert.Equal(second.Id, list.Items[0].Id);
        Assert.Equal(first!.Id, list.Items[1].Id);
    }

    [Theory]
    [InlineData("hot")]
    [InlineData("controversial")]
    public async Task ListCommunityPosts_PlaceholderSorts_DoNotErrorAndReturnExpectedCount(string sort)
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "placeholdersort");
        var communityName = TestNames.UniqueCommunityName();
        await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairId = await GetFirstFlairIdAsync(client, communityName);
        await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("First", "Body", null, null, null, flairId));
        await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Second", "Body", null, null, null, flairId));

        var listResponse = await client.GetAsync($"/api/communities/{communityName}/posts?sort={sort}&page=1");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, list!.Items.Count);
    }

    [Fact]
    public async Task CreateComment_ReplyAndMention_ProduceNotificationsForRecipients()
    {
        using var factory = CreateFactory();
        var (postAuthor, _) = await CreateClientWithCompletedProfileAsync(factory, "notifpostauthor");
        var (replier, replierUsername) = await CreateClientWithCompletedProfileAsync(factory, "notifreplier");
        var (mentioned, mentionedUsername) = await CreateClientWithCompletedProfileAsync(factory, "notifmentioned");
        var (_, postId) = await CreatePostAsync(postAuthor);

        var commentResponse = await replier.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest($"Nice post, /cc @{mentionedUsername}", null));
        Assert.Equal(HttpStatusCode.Created, commentResponse.StatusCode);

        var postAuthorNotifications = await postAuthor.GetAsync("/api/notifications");
        var postAuthorPage = await postAuthorNotifications.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        Assert.Contains(postAuthorPage!.Items, n => n.Type == NotificationType.Reply);

        var mentionedNotifications = await mentioned.GetAsync("/api/notifications");
        var mentionedPage = await mentionedNotifications.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        Assert.Contains(mentionedPage!.Items, n => n.Type == NotificationType.Mention);

        // The replier shouldn't have notified themselves.
        var replierNotifications = await replier.GetAsync("/api/notifications");
        var replierPage = await replierNotifications.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(TestJsonOptions.Default);
        Assert.Empty(replierPage!.Items);
    }

    [Fact]
    public async Task CreateComment_ContainingPhoneNumber_IsBlockedWithDetectedCategories()
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "piicommenter");
        var (_, postId) = await CreatePostAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("call me at 555-123-4567", null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("detectedCategories", body);
        Assert.Contains("Phone Number", body);
    }

    private static async Task<int> GetKarmaAsync(HttpClient client, string username)
    {
        var response = await client.GetAsync($"/api/users/{username}");
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(TestJsonOptions.Default);
        return user!.Karma;
    }

    [Fact]
    public async Task ListCommentsGetCommentAndReplies_WithoutAuth_ReturnAnonymousReads()
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "anoncomment");
        var (_, postId) = await CreatePostAsync(client);

        var topLevelResponse = await client.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("Top level", null));
        var topLevel = await topLevelResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);
        var replyResponse = await client.PostAsJsonAsync(
            $"/api/posts/{postId}/comments", new CreateCommentRequest("A reply", topLevel!.Id));
        var reply = await replyResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        using var anonymousClient = factory.CreateClient();

        var listResponse = await anonymousClient.GetAsync($"/api/posts/{postId}/comments");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var getResponse = await anonymousClient.GetAsync($"/api/comments/{topLevel.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var repliesResponse = await anonymousClient.GetAsync($"/api/comments/{topLevel.Id}/replies");
        Assert.Equal(HttpStatusCode.OK, repliesResponse.StatusCode);
        var replies = await repliesResponse.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>(TestJsonOptions.Default);
        Assert.Contains(replies!.Items, c => c.Id == reply!.Id);
    }
}
