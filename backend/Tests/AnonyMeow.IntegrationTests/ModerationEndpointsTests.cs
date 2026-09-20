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
using AnonyMeow.Dtos.Moderation;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ModerationEndpointsTests(PostgresContainerFixture postgresFixture)
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
        var response = await client.GetAsync($"/api/communities/{communityName}/flairs");
        response.EnsureSuccessStatusCode();
        var flairs = await response.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        return flairs![0].Id;
    }

    [Fact]
    public async Task ReportAndResolve_DismissAndActionTakenPaths()
    {
        using var factory = CreateFactory();
        var (mod, _) = await CreateClientWithCompletedProfileAsync(factory, "reportmod");
        var (reporter, _) = await CreateClientWithCompletedProfileAsync(factory, "reporter");
        var communityName = TestNames.UniqueCommunityName();
        await mod.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairId = await GetFirstFlairIdAsync(mod, communityName);
        var postResponse = await mod.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Post 1", "Body", null, null, null, flairId));
        var post1 = await postResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var report1Response = await reporter.PostAsJsonAsync($"/api/posts/{post1!.Id}/reports", new CreateReportRequest(ReportReasonCategory.Spam, "spam"));
        Assert.Equal(HttpStatusCode.Created, report1Response.StatusCode);
        var report1 = await report1Response.Content.ReadFromJsonAsync<ReportResponse>(TestJsonOptions.Default);

        var dismissResponse = await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/mod/reports/{report1!.Id}/resolve",
            new ResolveReportRequest(ReportOutcome.Dismiss, null));
        Assert.Equal(HttpStatusCode.OK, dismissResponse.StatusCode);
        var dismissed = await dismissResponse.Content.ReadFromJsonAsync<ReportResponse>(TestJsonOptions.Default);
        Assert.Equal(ReportStatus.Dismissed, dismissed!.Status);

        var post2Response = await mod.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Post 2", "Body", null, null, null, flairId));
        var post2 = await post2Response.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        var report2Response = await reporter.PostAsJsonAsync($"/api/posts/{post2!.Id}/reports", new CreateReportRequest(ReportReasonCategory.Other, "rule violation"));
        var report2 = await report2Response.Content.ReadFromJsonAsync<ReportResponse>(TestJsonOptions.Default);

        var actionTakenResponse = await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/mod/reports/{report2!.Id}/resolve",
            new ResolveReportRequest(ReportOutcome.ActionTaken, "removed"));
        Assert.Equal(HttpStatusCode.OK, actionTakenResponse.StatusCode);
        var actionTaken = await actionTakenResponse.Content.ReadFromJsonAsync<ReportResponse>(TestJsonOptions.Default);
        Assert.Equal(ReportStatus.ActionTaken, actionTaken!.Status);

        // Non-mod cannot resolve.
        var nonModResolve = await reporter.PostAsJsonAsync(
            $"/api/communities/{communityName}/mod/reports/{report1.Id}/resolve",
            new ResolveReportRequest(ReportOutcome.Dismiss, null));
        Assert.Equal(HttpStatusCode.Forbidden, nonModResolve.StatusCode);
    }

    [Fact]
    public async Task ResolveReport_FromAnotherCommunity_404s_EvenForThatCallersOwnCommunity()
    {
        // A moderator of communityA must not be able to resolve a report that belongs to
        // communityB just because they know its ID (e.g. by having filed it themselves).
        using var factory = CreateFactory();
        var (modA, _) = await CreateClientWithCompletedProfileAsync(factory, "crossmoda");
        var (modB, _) = await CreateClientWithCompletedProfileAsync(factory, "crossmodb");
        var communityAName = TestNames.UniqueCommunityName();
        var communityBName = TestNames.UniqueCommunityName();
        await modA.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityAName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        await modB.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityBName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairIdB = await GetFirstFlairIdAsync(modB, communityBName);

        var postResponse = await modB.PostAsJsonAsync($"/api/communities/{communityBName}/posts",
            new CreatePostRequest("Post in B", "Body", null, null, null, flairIdB));
        var postB = await postResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var reportResponse = await modA.PostAsJsonAsync($"/api/posts/{postB!.Id}/reports", new CreateReportRequest(ReportReasonCategory.Spam, "spam"));
        var report = await reportResponse.Content.ReadFromJsonAsync<ReportResponse>(TestJsonOptions.Default);

        // modA is a real moderator of communityA, but the report belongs to communityB.
        var crossResolve = await modA.PostAsJsonAsync(
            $"/api/communities/{communityAName}/mod/reports/{report!.Id}/resolve",
            new ResolveReportRequest(ReportOutcome.Dismiss, null));
        Assert.Equal(HttpStatusCode.NotFound, crossResolve.StatusCode);

        // The report is untouched and still resolvable by communityB's own moderator.
        var properResolve = await modB.PostAsJsonAsync(
            $"/api/communities/{communityBName}/mod/reports/{report.Id}/resolve",
            new ResolveReportRequest(ReportOutcome.Dismiss, null));
        Assert.Equal(HttpStatusCode.OK, properResolve.StatusCode);
    }

    [Fact]
    public async Task PinLockRemove_Lifecycle_ModOnly()
    {
        using var factory = CreateFactory();
        var (mod, _) = await CreateClientWithCompletedProfileAsync(factory, "plrmod");
        var (other, _) = await CreateClientWithCompletedProfileAsync(factory, "plrother");
        var communityName = TestNames.UniqueCommunityName();
        await mod.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairId = await GetFirstFlairIdAsync(mod, communityName);
        var postResponse = await mod.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Post", "Body", null, null, null, flairId));
        var post = await postResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var nonModPin = await other.PostAsync($"/api/posts/{post!.Id}/mod/pin", null);
        Assert.Equal(HttpStatusCode.Forbidden, nonModPin.StatusCode);

        var pinResponse = await mod.PostAsync($"/api/posts/{post.Id}/mod/pin", null);
        Assert.Equal(HttpStatusCode.NoContent, pinResponse.StatusCode);
        var afterPin = await mod.GetAsync($"/api/posts/{post.Id}");
        Assert.True((await afterPin.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default))!.IsPinned);

        var unpinResponse = await mod.DeleteAsync($"/api/posts/{post.Id}/mod/pin");
        Assert.Equal(HttpStatusCode.NoContent, unpinResponse.StatusCode);

        var lockResponse = await mod.PostAsync($"/api/posts/{post.Id}/mod/lock", null);
        Assert.Equal(HttpStatusCode.NoContent, lockResponse.StatusCode);
        var afterLock = await mod.GetAsync($"/api/posts/{post.Id}");
        Assert.True((await afterLock.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default))!.IsLocked);

        // Locked post rejects new comments.
        var commentOnLocked = await mod.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Nope", null));
        Assert.Equal(HttpStatusCode.Forbidden, commentOnLocked.StatusCode);

        var removeResponse = await mod.PostAsync($"/api/posts/{post.Id}/mod/remove", null);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
        var afterRemove = await other.GetAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterRemove.StatusCode);
    }

    [Fact]
    public async Task BanEnforcement_BlocksPostingUntilUnbanned()
    {
        using var factory = CreateFactory();
        var (mod, _) = await CreateClientWithCompletedProfileAsync(factory, "banmod");
        var (target, targetUsername) = await CreateClientWithCompletedProfileAsync(factory, "bantarget");
        var communityName = TestNames.UniqueCommunityName();
        await mod.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairId = await GetFirstFlairIdAsync(mod, communityName);
        await target.PostAsync($"/api/communities/{communityName}/join", null);

        // Posting succeeds before the ban.
        var beforeBan = await target.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Before ban", "Body", null, null, null, flairId));
        Assert.Equal(HttpStatusCode.Created, beforeBan.StatusCode);

        var banResponse = await mod.PostAsJsonAsync(
            $"/api/communities/{communityName}/mod/bans", new CreateBanRequest(targetUsername, "harassment"));
        Assert.Equal(HttpStatusCode.Created, banResponse.StatusCode);

        var listBansResponse = await mod.GetAsync($"/api/communities/{communityName}/mod/bans");
        var bans = await listBansResponse.Content.ReadFromJsonAsync<List<BanResponse>>(TestJsonOptions.Default);
        Assert.Contains(bans!, b => b.Username == targetUsername);

        // Banned user's subsequent post attempt returns 403 — proves the 1.6.3 retrofit.
        var blockedPost = await target.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("After ban", "Body", null, null, null, flairId));
        Assert.Equal(HttpStatusCode.Forbidden, blockedPost.StatusCode);

        // Also blocked from commenting on the earlier post.
        var beforeBanPost = await beforeBan.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        var blockedComment = await target.PostAsJsonAsync(
            $"/api/posts/{beforeBanPost!.Id}/comments", new CreateCommentRequest("Blocked", null));
        Assert.Equal(HttpStatusCode.Forbidden, blockedComment.StatusCode);

        var unbanResponse = await mod.DeleteAsync($"/api/communities/{communityName}/mod/bans/{targetUsername}");
        Assert.Equal(HttpStatusCode.NoContent, unbanResponse.StatusCode);

        // Posting succeeds again after unban.
        var afterUnban = await target.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("After unban", "Body", null, null, null, flairId));
        Assert.Equal(HttpStatusCode.Created, afterUnban.StatusCode);
    }

    [Fact]
    public async Task PromoteAndDemoteModerator_Lifecycle_ModOnly()
    {
        using var factory = CreateFactory();
        var (mod, modUsername) = await CreateClientWithCompletedProfileAsync(factory, "pdmod");
        var (member, memberUsername) = await CreateClientWithCompletedProfileAsync(factory, "pdmember");
        var (other, _) = await CreateClientWithCompletedProfileAsync(factory, "pdother");
        var communityName = TestNames.UniqueCommunityName();
        await mod.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        await member.PostAsync($"/api/communities/{communityName}/join", null);

        // Non-mod cannot promote.
        var nonModPromote = await other.PostAsync($"/api/communities/{communityName}/mod/moderators/{memberUsername}", null);
        Assert.Equal(HttpStatusCode.Forbidden, nonModPromote.StatusCode);

        // Promoting a user with no membership row 404s.
        var notFoundPromote = await mod.PostAsync($"/api/communities/{communityName}/mod/moderators/{TestNames.UniqueCommunityName(10)}", null);
        Assert.Equal(HttpStatusCode.NotFound, notFoundPromote.StatusCode);

        var promoteResponse = await mod.PostAsync($"/api/communities/{communityName}/mod/moderators/{memberUsername}", null);
        Assert.Equal(HttpStatusCode.NoContent, promoteResponse.StatusCode);

        var moderatorsResponse = await mod.GetAsync($"/api/communities/{communityName}/moderators");
        var moderators = await moderatorsResponse.Content.ReadFromJsonAsync<List<CommunityModeratorResponse>>(TestJsonOptions.Default);
        Assert.Contains(moderators!, m => m.Username == memberUsername);

        // The newly promoted moderator now has moderator authority — can demote the original creator.
        var demoteModResponse = await member.DeleteAsync($"/api/communities/{communityName}/mod/moderators/{modUsername}");
        Assert.Equal(HttpStatusCode.NoContent, demoteModResponse.StatusCode);

        var moderatorsAfterDemote = await mod.GetAsync($"/api/communities/{communityName}/moderators");
        var moderatorsList = await moderatorsAfterDemote.Content.ReadFromJsonAsync<List<CommunityModeratorResponse>>(TestJsonOptions.Default);
        Assert.DoesNotContain(moderatorsList!, m => m.Username == modUsername);

        // Only one moderator (member) remains — demoting them is blocked.
        var soleModDemote = await member.DeleteAsync($"/api/communities/{communityName}/mod/moderators/{memberUsername}");
        Assert.Equal(HttpStatusCode.Conflict, soleModDemote.StatusCode);
    }

    [Fact]
    public async Task RemoveComment_ModOnly_ThenCommentIsGoneFromPublicView()
    {
        using var factory = CreateFactory();
        var (mod, _) = await CreateClientWithCompletedProfileAsync(factory, "crmod");
        var (author, _) = await CreateClientWithCompletedProfileAsync(factory, "crauthor");
        var communityName = TestNames.UniqueCommunityName();
        await mod.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(communityName, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        var flairId = await GetFirstFlairIdAsync(mod, communityName);
        var postResponse = await mod.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Post", "Body", null, null, null, flairId));
        var post = await postResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var commentResponse = await author.PostAsJsonAsync(
            $"/api/posts/{post!.Id}/comments", new CreateCommentRequest("Hello", null));
        var comment = await commentResponse.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);

        // Not a moderator, not the author's own delete route — forbidden.
        var nonModRemove = await author.PostAsync($"/api/comments/{comment!.Id}/mod/remove", null);
        Assert.Equal(HttpStatusCode.Forbidden, nonModRemove.StatusCode);

        var removeResponse = await mod.PostAsync($"/api/comments/{comment.Id}/mod/remove", null);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var afterRemove = await mod.GetAsync($"/api/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterRemove.StatusCode);
    }
}
