using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Admin;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Moderation;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AdminEndpointsTests(PostgresContainerFixture postgresFixture)
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

    // IsPlatformAdmin is a DB-only flag, manually provisioned per the parent plan — no API sets
    // it, so tests seed it directly the same way ConversationAndMessageEndpointsTests reaches
    // into the DbContext via factory.Services.
    private static async Task MakePlatformAdminAsync(CustomWebApplicationFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalized = username.ToLowerInvariant();
        var user = await dbContext.Users.SingleAsync(u => u.Username != null && u.Username.ToLower() == normalized);
        user.IsPlatformAdmin = true;
        await dbContext.SaveChangesAsync();
    }

    private static async Task<(string Name, Guid FlairId)> CreateCommunityAsync(HttpClient client)
    {
        var name = TestNames.UniqueCommunityName();
        var response = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        response.EnsureSuccessStatusCode();

        var flairsResponse = await client.GetAsync($"/api/communities/{name}/flairs");
        flairsResponse.EnsureSuccessStatusCode();
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        return (name, flairs![0].Id);
    }

    [Fact]
    public async Task AdminEndpoints_NonAdmin_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        var (client, _) = await CreateClientWithCompletedProfileAsync(factory, "notadmin");

        var response = await client.GetAsync("/api/admin/reports");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_PlatformAdmin_CanListReportsSpamFlagsAndAuditLog()
    {
        using var factory = CreateFactory();
        var (admin, adminUsername) = await CreateClientWithCompletedProfileAsync(factory, "reallyadmin");
        await MakePlatformAdminAsync(factory, adminUsername);

        var reportsResponse = await admin.GetAsync("/api/admin/reports");
        Assert.Equal(HttpStatusCode.OK, reportsResponse.StatusCode);
        Assert.NotNull(await reportsResponse.Content.ReadFromJsonAsync<PagedResponse<ReportResponse>>(TestJsonOptions.Default));

        var spamFlagsResponse = await admin.GetAsync("/api/admin/spam-flags");
        Assert.Equal(HttpStatusCode.OK, spamFlagsResponse.StatusCode);
        Assert.NotNull(await spamFlagsResponse.Content.ReadFromJsonAsync<PagedResponse<SpamFlagResponse>>(TestJsonOptions.Default));

        var auditLogResponse = await admin.GetAsync("/api/admin/audit-log");
        Assert.Equal(HttpStatusCode.OK, auditLogResponse.StatusCode);
        Assert.NotNull(await auditLogResponse.Content.ReadFromJsonAsync<PagedResponse<ModerationActionResponse>>(TestJsonOptions.Default));
    }

    [Fact]
    public async Task RestrictUser_NonExistentUsername_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (admin, adminUsername) = await CreateClientWithCompletedProfileAsync(factory, "restrictadmin");
        await MakePlatformAdminAsync(factory, adminUsername);

        var response = await admin.PostAsJsonAsync(
            "/api/admin/users/no-such-user/restrict",
            new CreatePlatformRestrictionRequest(PlatformRestrictionType.PostingRestricted, "test", null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RestrictUser_BlocksPosting_ThenLiftRestriction_AllowsPostingAgain()
    {
        using var factory = CreateFactory();
        var (admin, adminUsername) = await CreateClientWithCompletedProfileAsync(factory, "liftadmin");
        await MakePlatformAdminAsync(factory, adminUsername);
        var (target, targetUsername) = await CreateClientWithCompletedProfileAsync(factory, "restricttarget");
        var (communityName, flairId) = await CreateCommunityAsync(target);

        var restrictResponse = await admin.PostAsJsonAsync(
            $"/api/admin/users/{targetUsername}/restrict",
            new CreatePlatformRestrictionRequest(PlatformRestrictionType.PostingRestricted, "rule violation", null));
        Assert.Equal(HttpStatusCode.Created, restrictResponse.StatusCode);
        var restriction = await restrictResponse.Content.ReadFromJsonAsync<PlatformRestrictionResponse>(TestJsonOptions.Default);
        Assert.Equal(PlatformRestrictionStatus.Active, restriction!.Status);

        var blockedPostResponse = await target.PostAsJsonAsync(
            $"/api/communities/{communityName}/posts", new CreatePostRequest("Should be blocked", "Body", null, null, null, flairId));
        Assert.Equal(HttpStatusCode.Forbidden, blockedPostResponse.StatusCode);

        var liftResponse = await admin.DeleteAsync($"/api/admin/users/{targetUsername}/restrict");
        Assert.Equal(HttpStatusCode.NoContent, liftResponse.StatusCode);

        var allowedPostResponse = await target.PostAsJsonAsync(
            $"/api/communities/{communityName}/posts", new CreatePostRequest("Should succeed now", "Body", null, null, null, flairId));
        Assert.Equal(HttpStatusCode.Created, allowedPostResponse.StatusCode);

        var auditLogResponse = await admin.GetAsync("/api/admin/audit-log");
        var auditLog = await auditLogResponse.Content.ReadFromJsonAsync<PagedResponse<ModerationActionResponse>>(TestJsonOptions.Default);
        Assert.Contains(auditLog!.Items, a => a.ActionType == ModerationActionType.PlatformRestrict);
        Assert.Contains(auditLog.Items, a => a.ActionType == ModerationActionType.PlatformRestrictionLifted);
    }
}
