using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class PostEndpointsTests(PostgresContainerFixture postgresFixture)
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

    private static async Task<string> CreateCommunityAsync(HttpClient client)
    {
        var name = TestNames.UniqueCommunityName();
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
    public async Task CreatePost_TitleOverMaxLength_ReturnsValidationProblem()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"longtitle{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(client);
        var flairId = await GetFirstFlairIdAsync(client, communityName);

        var response = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest(new string('a', 301), "Body", null, null, null, flairId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // Images are exercised at the unit level (PostServiceTests) rather than here — Azure Blob
    // Storage isn't configured in the integration test environment (see
    // ImageUploadSas_WithoutStorageConfigured_ReturnsServiceUnavailable below), so any
    // integration request carrying ImageUrls would 503 on the size-check call rather than
    // exercising the attachment-combination path this suite targets.
    // FlairId is a placeholder (Guid.Empty) here — MemberData is generated statically at test
    // discovery time, before any community/flair exists. Each test invocation swaps in a real,
    // community-scoped flair id via `with { FlairId = ... }` before sending the request.
    public static IEnumerable<object[]> HappyPathPostCases()
    {
        yield return [new CreatePostRequest("A body-only post", "Some body", null, null, null, Guid.Empty)];
        yield return [new CreatePostRequest("A link-only post", null, "https://example.com", null, null, Guid.Empty)];
        yield return [new CreatePostRequest("A poll-only post", null, null, null, new List<string> { "Option A", "Option B" }, Guid.Empty)];
        yield return [new CreatePostRequest(
            "A combined post", "Some body", "https://example.com", null, new List<string> { "Option A", "Option B" }, Guid.Empty)];
    }

    [Theory]
    [MemberData(nameof(HappyPathPostCases))]
    public async Task CreatePost_HappyPath_ReturnsCreated(CreatePostRequest request)
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"poster{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(client);
        var flairId = await GetFirstFlairIdAsync(client, communityName);
        request = request with { FlairId = flairId };

        var response = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        Assert.Equal(request.Title, body!.Title);
        Assert.Equal(request.BodyMarkdown, body.BodyMarkdown);
        Assert.Equal(request.Url, body.Url);
        Assert.Equal(request.PollOptions is not null, body.PollOptions is not null);
    }

    // FlairId is a placeholder (Guid.Empty) here — see the comment on HappyPathPostCases above.
    // A real, community-scoped flair id is swapped in per-invocation so these cases fail for the
    // reason under test (validation), not because of an unrelated bad flair id.
    public static IEnumerable<object[]> ValidationFailureCases()
    {
        yield return [new CreatePostRequest("Nothing but a title", null, null, null, null, Guid.Empty)];
        yield return [new CreatePostRequest("Malformed url", null, "/relative/path", null, null, Guid.Empty)];
        yield return [new CreatePostRequest("Too few options", null, null, null, new List<string> { "OnlyOne" }, Guid.Empty)];
        yield return [new CreatePostRequest(
            "Too many images", null, null, Enumerable.Range(0, 7).Select(i => $"https://blob.example/{i}.png").ToList(), null, Guid.Empty)];
    }

    [Theory]
    [MemberData(nameof(ValidationFailureCases))]
    public async Task CreatePost_ValidationFailure_ReturnsUnprocessableEntity(CreatePostRequest request)
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"badpost{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(client);
        var flairId = await GetFirstFlairIdAsync(client, communityName);
        request = request with { FlairId = flairId };

        var response = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetPatchDelete_Lifecycle()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"lifecycle{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(client);
        var flairId = await GetFirstFlairIdAsync(client, communityName);

        var createResponse = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Original title", null, "https://example.com/original", null, null, flairId));
        var created = await createResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var getResponse = await client.GetAsync($"/api/posts/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var patchResponse = await client.PatchAsJsonAsync($"/api/posts/{created.Id}",
            new UpdatePostRequest("Updated title", null, "https://example.com/updated"));
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var patched = await patchResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        Assert.Equal("Updated title", patched!.Title);
        Assert.Equal("https://example.com/updated", patched.Url);

        var blankTitleResponse = await client.PatchAsJsonAsync($"/api/posts/{created.Id}",
            new UpdatePostRequest("   ", null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blankTitleResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/posts/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Removed post is 404 to a fresh non-author viewer.
        using var otherClient = await CreateClientWithCompletedProfileAsync(factory, $"viewer{Guid.NewGuid():N}"[..15]);
        var hiddenResponse = await otherClient.GetAsync($"/api/posts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);

        // Still visible to the author.
        var stillVisibleToAuthor = await client.GetAsync($"/api/posts/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, stillVisibleToAuthor.StatusCode);
    }

    [Fact]
    public async Task PollVote_HappyPath_AndDuplicateVoteChangesSelection()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"pollowner{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(client);
        var flairId = await GetFirstFlairIdAsync(client, communityName);
        var createResponse = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Vote here", null, null, null, new List<string> { "Red", "Blue" }, flairId));
        var created = await createResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        var redOptionId = created!.PollOptions!.First(o => o.Text == "Red").Id;
        var blueOptionId = created.PollOptions!.First(o => o.Text == "Blue").Id;

        using var voterClient = await CreateClientWithCompletedProfileAsync(factory, $"voter{Guid.NewGuid():N}"[..15]);

        var firstVoteResponse = await voterClient.PostAsJsonAsync(
            $"/api/posts/{created.Id}/poll-votes", new CastPollVoteRequest(redOptionId));
        Assert.Equal(HttpStatusCode.OK, firstVoteResponse.StatusCode);
        var afterFirstVote = (await firstVoteResponse.Content.ReadFromJsonAsync<List<PollOptionResponse>>(TestJsonOptions.Default))!;
        Assert.Equal(1, afterFirstVote.First(o => o.Id == redOptionId).VoteCount);
        Assert.Equal(0, afterFirstVote.First(o => o.Id == blueOptionId).VoteCount);

        // Duplicate vote from the same voter changes their selection rather than adding a second vote.
        var secondVoteResponse = await voterClient.PostAsJsonAsync(
            $"/api/posts/{created.Id}/poll-votes", new CastPollVoteRequest(blueOptionId));
        Assert.Equal(HttpStatusCode.OK, secondVoteResponse.StatusCode);
        var afterSecondVote = (await secondVoteResponse.Content.ReadFromJsonAsync<List<PollOptionResponse>>(TestJsonOptions.Default))!;
        Assert.Equal(0, afterSecondVote.First(o => o.Id == redOptionId).VoteCount);
        Assert.Equal(1, afterSecondVote.First(o => o.Id == blueOptionId).VoteCount);
    }

    [Fact]
    public async Task ImageUploadSas_WithoutStorageConfigured_ReturnsServiceUnavailable()
    {
        // Shape-only check: Azure Blob Storage is not provisioned yet (open item pending user
        // confirmation, same as the Phase 0 B2C checkpoint), so the only verifiable behavior
        // right now is that the endpoint exists, is authenticated, and fails predictably.
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"uploader{Guid.NewGuid():N}"[..15]);

        var response = await client.PostAsync("/api/uploads/images/sas", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_ContainingEmail_IsBlockedWithDetectedCategories()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"piiposter{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(client);
        var flairId = await GetFirstFlairIdAsync(client, communityName);

        var response = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Contact me", "email me at someone@example.com", null, null, null, flairId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("detectedCategories", body);
        Assert.Contains("Email", body);
    }

    [Fact]
    public async Task ListPostsAndGetPost_WithoutAuth_ReturnAnonymousReads()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"anonpost{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(client);
        var flairId = await GetFirstFlairIdAsync(client, communityName);
        var createResponse = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Anonymously readable", "Body", null, null, null, flairId));
        var created = await createResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        using var anonymousClient = factory.CreateClient();

        var listResponse = await anonymousClient.GetAsync($"/api/communities/{communityName}/posts");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(list!.Items, p => p.Id == created!.Id);

        var getResponse = await anonymousClient.GetAsync($"/api/posts/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task ListPosts_SortPinned_ReturnsOnlyPinnedPosts_AnonymousReadable()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"pinlist{Guid.NewGuid():N}"[..15]);
        var communityName = await CreateCommunityAsync(client);
        var flairId = await GetFirstFlairIdAsync(client, communityName);

        var unpinnedResponse = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Not pinned", "Body", null, null, null, flairId));
        var unpinned = await unpinnedResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);

        var pinnedResponse = await client.PostAsJsonAsync($"/api/communities/{communityName}/posts",
            new CreatePostRequest("Pinned", "Body", null, null, null, flairId));
        var pinned = await pinnedResponse.Content.ReadFromJsonAsync<PostResponse>(TestJsonOptions.Default);
        var pinAction = await client.PostAsync($"/api/posts/{pinned!.Id}/mod/pin", null);
        Assert.Equal(HttpStatusCode.NoContent, pinAction.StatusCode);

        using var anonymousClient = factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/communities/{communityName}/posts?sort=pinned");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pinnedPage = await response.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>(TestJsonOptions.Default);
        Assert.Contains(pinnedPage!.Items, p => p.Id == pinned.Id);
        Assert.DoesNotContain(pinnedPage.Items, p => p.Id == unpinned!.Id);
        Assert.All(pinnedPage.Items, p => Assert.True(p.IsPinned));
    }
}
