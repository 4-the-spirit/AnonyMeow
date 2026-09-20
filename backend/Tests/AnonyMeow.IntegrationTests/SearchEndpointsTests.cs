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
using AnonyMeow.Dtos.Search;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class SearchEndpointsTests(PostgresContainerFixture postgresFixture)
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

    private static async Task<string> CreateCommunityAsync(HttpClient client, string? name = null)
    {
        name ??= TestNames.UniqueCommunityName();
        var response = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, "A community about kayaking", null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        response.EnsureSuccessStatusCode();
        return name;
    }

    private static async Task<Guid> CreatePostAsync(HttpClient client, string communityName, string title, string body = "Body")
    {
        var flairsResponse = await client.GetAsync($"/api/communities/{communityName}/flairs");
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);

        var response = await client.PostAsJsonAsync(
            $"/api/communities/{communityName}/posts", new CreatePostRequest(title, body, null, null, null, flairs![0].Id));
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        return responseBody!.Id;
    }

    private static async Task<Guid> CreateCommentAsync(HttpClient client, Guid postId, string body)
    {
        var response = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreateCommentRequest(body, null));
        response.EnsureSuccessStatusCode();
        var comment = await response.Content.ReadFromJsonAsync<CommentResponse>(TestJsonOptions.Default);
        return comment!.Id;
    }

    [Fact]
    public async Task Search_FindsPostByTitle()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searcha");
        var communityName = await CreateCommunityAsync(client);
        var matching = await CreatePostAsync(client, communityName, "Astronomy for beginners", "Body");
        var nonMatching = await CreatePostAsync(client, communityName, "Cooking pasta", "Body");

        var response = await client.GetAsync("/api/search?q=astronomy&type=posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(TestJsonOptions.Default);
        Assert.Contains(result!.Posts, p => p.Id == matching);
        Assert.DoesNotContain(result.Posts, p => p.Id == nonMatching);
        Assert.Empty(result.Comments);
        Assert.Empty(result.Communities);
    }

    [Fact]
    public async Task Search_FindsPostByTitle_SubstringNotCaughtByFullText()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searchsub");
        var communityName = await CreateCommunityAsync(client);
        var token = $"zx{Guid.NewGuid():N}"[..15];
        var matching = await CreatePostAsync(client, communityName, $"pre{token}post", "Body");

        // "pre{token}post" has no lexeme match for "{token}" via websearch_to_tsquery (it's
        // embedded inside a single larger word) — only the ILike substring match should find it.
        var response = await client.GetAsync($"/api/search?q={Uri.EscapeDataString(token)}&type=posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(TestJsonOptions.Default);
        Assert.Contains(result!.Posts, p => p.Id == matching);
    }

    [Fact]
    public async Task Search_FindsPostByTitle_WhenSearchTextContainsTitle_ReverseDirection()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searchrev");
        var communityName = await CreateCommunityAsync(client);
        var shortTitle = $"nv{Guid.NewGuid():N}"[..8];
        var matching = await CreatePostAsync(client, communityName, shortTitle, "Body");
        var longerSearchText = $"pre{shortTitle}post";

        var response = await client.GetAsync($"/api/search?q={Uri.EscapeDataString(longerSearchText)}&type=posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(TestJsonOptions.Default);
        Assert.Contains(result!.Posts, p => p.Id == matching);
    }

    [Fact]
    public async Task Search_FindsCommentByBody()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searchb");
        var communityName = await CreateCommunityAsync(client);
        var postId = await CreatePostAsync(client, communityName, "A post");
        var matchingComment = await CreateCommentAsync(client, postId, "I love hiking in the mountains");
        var nonMatchingComment = await CreateCommentAsync(client, postId, "What time is the meeting");

        var response = await client.GetAsync("/api/search?q=hiking&type=comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(TestJsonOptions.Default);
        Assert.Contains(result!.Comments, c => c.Id == matchingComment);
        Assert.DoesNotContain(result.Comments, c => c.Id == nonMatchingComment);
    }

    [Fact]
    public async Task Search_FindsCommunityByName()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searchc");
        var uniqueToken = TestNames.UniqueCommunityName(20);
        var communityName = await CreateCommunityAsync(client, uniqueToken);

        var response = await client.GetAsync($"/api/search?q={Uri.EscapeDataString(uniqueToken)}&type=communities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(TestJsonOptions.Default);
        Assert.Contains(result!.Communities, c => c.Name == communityName);
    }

    [Fact]
    public async Task Search_FindsCommunityByName_WhenSearchTextContainsName_ReverseDirection()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searchcrev");
        var uniqueToken = TestNames.UniqueCommunityName(10);
        var communityName = await CreateCommunityAsync(client, uniqueToken);
        var longerSearchText = $"אב{uniqueToken}גד";

        var response = await client.GetAsync($"/api/search?q={Uri.EscapeDataString(longerSearchText)}&type=communities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(TestJsonOptions.Default);
        Assert.Contains(result!.Communities, c => c.Name == communityName);
    }

    [Fact]
    public async Task Search_TypeAll_PopulatesAllThreeCategories()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searchd");
        var uniqueToken = $"zephyr{Guid.NewGuid():N}"[..15];
        var communityName = await CreateCommunityAsync(client);
        var postId = await CreatePostAsync(client, communityName, $"About {uniqueToken}", "Body");
        await CreateCommentAsync(client, postId, $"Discussing {uniqueToken} further");

        var response = await client.GetAsync($"/api/search?q={uniqueToken}&type=all");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(TestJsonOptions.Default);
        Assert.NotEmpty(result!.Posts);
        Assert.NotEmpty(result.Comments);
    }

    [Fact]
    public async Task Search_MissingQuery_ReturnsUnprocessableEntity()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searche");

        var response = await client.GetAsync("/api/search?type=posts");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithoutAuth_ReturnsAnonymousRead()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, "searchf");
        var communityName = await CreateCommunityAsync(client);
        var matching = await CreatePostAsync(client, communityName, "Anonymously findable post", "Body");

        using var anonymousClient = factory.CreateClient();
        var response = await anonymousClient.GetAsync("/api/search?q=anonymously&type=posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>(TestJsonOptions.Default);
        Assert.Contains(result!.Posts, p => p.Id == matching);
    }
}
