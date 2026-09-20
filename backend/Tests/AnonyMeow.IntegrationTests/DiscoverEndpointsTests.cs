using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class DiscoverEndpointsTests(PostgresContainerFixture postgresFixture)
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
        flairsResponse.EnsureSuccessStatusCode();
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);

        var response = await client.PostAsJsonAsync(
            $"/api/communities/{communityName}/posts", new CreatePostRequest(title, "Body", null, null, null, flairs![0].Id));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        return body!.Id;
    }

    // The trending window is relative to CreatedAtUtc, which the API always stamps as "now" — so
    // backdating requires reaching into the DbContext directly, the same pattern
    // AdminEndpointsTests uses to seed IsPlatformAdmin.
    private static async Task BackdatePostAsync(CustomWebApplicationFactory factory, Guid postId, TimeSpan age)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.SingleAsync(p => p.Id == postId);
        post.CreatedAtUtc = DateTimeOffset.UtcNow - age;
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Trending_DefaultDayWindow_ExcludesPostsOlderThanADay()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "trenda");
        var communityName = await CreateCommunityAsync(client);
        var recentPost = await CreatePostAsync(client, communityName, "Recent post");
        var oldPost = await CreatePostAsync(client, communityName, "Old post");
        await BackdatePostAsync(factory, oldPost, TimeSpan.FromDays(2));

        // Scoped to this test's own community: the shared Postgres fixture accumulates posts
        // across every test in the collection, so an unscoped platform-wide query at the default
        // page size can't reliably assert presence/absence without flaking on run order.
        var response = await client.GetAsync($"/api/discover/trending?scope=community&community={communityName}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(page!.Items, p => p.Id == recentPost);
        Assert.DoesNotContain(page.Items, p => p.Id == oldPost);
    }

    [Fact]
    public async Task Trending_WeekWindow_IncludesPostFromTwoDaysAgo()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "trendb");
        var communityName = await CreateCommunityAsync(client);
        var post = await CreatePostAsync(client, communityName, "Two days old");
        await BackdatePostAsync(factory, post, TimeSpan.FromDays(2));

        var response = await client.GetAsync($"/api/discover/trending?scope=community&community={communityName}&window=week");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(page!.Items, p => p.Id == post);
    }

    [Fact]
    public async Task Trending_ScopedToCommunity_ExcludesOtherCommunityPosts()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "trendc");
        var communityA = await CreateCommunityAsync(client);
        var communityB = await CreateCommunityAsync(client);
        var postA = await CreatePostAsync(client, communityA, "In A");
        var postB = await CreatePostAsync(client, communityB, "In B");

        var response = await client.GetAsync($"/api/discover/trending?scope=community&community={communityA}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(page!.Items, p => p.Id == postA);
        Assert.DoesNotContain(page.Items, p => p.Id == postB);
    }

    [Fact]
    public async Task Trending_CommunityScope_UnknownCommunity_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "trendd");

        var response = await client.GetAsync("/api/discover/trending?scope=community&community=doesnotexist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecommendedCommunities_ExcludesAlreadyJoinedCommunities()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "recoa");
        var joinedCommunity = await CreateCommunityAsync(client); // creator auto-joins
        using var otherClient = await CreateClientWithCompletedProfileAsync(factory, "recob");
        var notJoinedCommunity = await CreateCommunityAsync(otherClient);

        var response = await client.GetAsync("/api/discover/recommended-communities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<CommunityResponse>>(TestJsonOptions.Default);
        Assert.Contains(page!.Items, c => c.Name == notJoinedCommunity);
        Assert.DoesNotContain(page.Items, c => c.Name == joinedCommunity);
    }

    [Fact]
    public async Task TrendingAndRecommendedCommunities_WithoutAuth_ReturnAnonymousReads()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "anontrend");
        var communityName = await CreateCommunityAsync(client);
        await CreatePostAsync(client, communityName, "Anonymously trending");

        using var anonymousClient = factory.CreateClient();

        var trendingResponse = await anonymousClient.GetAsync(
            $"/api/discover/trending?scope=community&community={communityName}");
        Assert.Equal(HttpStatusCode.OK, trendingResponse.StatusCode);

        // No joined-communities exclusion is possible for an anonymous viewer — this just
        // confirms the endpoint doesn't throw trying to resolve a current user.
        var recommendedResponse = await anonymousClient.GetAsync("/api/discover/recommended-communities");
        Assert.Equal(HttpStatusCode.OK, recommendedResponse.StatusCode);
        var page = await recommendedResponse.Content.ReadFromJsonAsync<PagedResponse<CommunityResponse>>(TestJsonOptions.Default);
        Assert.Contains(page!.Items, c => c.Name == communityName);
    }
}
