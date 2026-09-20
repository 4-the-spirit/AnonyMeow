using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Moderation;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class SpamDetectionAndRateLimitEndpointsTests(PostgresContainerFixture postgresFixture)
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

    private static async Task<(string Name, Guid FlairId)> CreateCommunityAsync(HttpClient client)
    {
        var name = TestNames.UniqueCommunityName();
        var response = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        response.EnsureSuccessStatusCode();

        var flairsResponse = await client.GetAsync($"/api/communities/{name}/flairs");
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        return (name, flairs![0].Id);
    }

    [Fact]
    public async Task CreatePost_ExceedsNamedRateLimitPolicy_ReturnsTooManyRequests()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "ratelimit");
        var (communityName, flairId) = await CreateCommunityAsync(client);

        // appsettings.json's "CreatePost" named policy defaults to PermitLimit=5 within a
        // 300-second fixed window — the 6th create in the same window is rejected.
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 6; i++)
        {
            lastResponse = await client.PostAsJsonAsync(
                $"/api/communities/{communityName}/posts",
                new CreatePostRequest($"Post {i}", $"Body {i}", null, null, null, flairId));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }

    [Fact]
    public async Task ImageUploadSas_ExceedsNamedRateLimitPolicy_ReturnsTooManyRequests()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "imgratelimit");

        // appsettings.json's "ImageUpload" named policy defaults to PermitLimit=30 within an
        // 86400-second (24h) fixed window — the 31st mint in the same window is rejected. Rate
        // limiting is enforced by middleware ahead of the endpoint, so this 429s even though Blob
        // Storage isn't configured in this environment (which would otherwise 503 the handler).
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 31; i++)
        {
            lastResponse = await client.PostAsync("/api/uploads/images/sas", null);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }

    [Fact]
    public async Task CreatePost_DuplicateContent_FilesAutomatedSpamReport_VisibleInModQueue()
    {
        using var factory = CreateFactory();
        using var author = await CreateClientWithCompletedProfileAsync(factory, "spamauthor");
        var (communityName, flairId) = await CreateCommunityAsync(author);
        var duplicateRequest = new CreatePostRequest("Repeated Title", "Repeated body text", null, null, null, flairId);

        var first = await author.PostAsJsonAsync($"/api/communities/{communityName}/posts", duplicateRequest);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var second = await author.PostAsJsonAsync($"/api/communities/{communityName}/posts", duplicateRequest);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondPost = await second.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        // The community creator is auto-joined as a moderator (CommunityService.CreateAsync), so
        // the same client can read its own mod queue.
        var reportsResponse = await author.GetAsync($"/api/communities/{communityName}/mod/reports");
        Assert.Equal(HttpStatusCode.OK, reportsResponse.StatusCode);
        var reports = await reportsResponse.Content.ReadFromJsonAsync<PagedResponse<ReportResponse>>(TestJsonOptions.Default);

        Assert.Contains(reports!.Items, r =>
            r.TargetId == secondPost!.Id && r.Status == ReportStatus.Open &&
            r.Details != null && r.Details.Contains("Automated spam detection"));
    }
}
