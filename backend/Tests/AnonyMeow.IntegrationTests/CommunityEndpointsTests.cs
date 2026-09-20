using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Common;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class CommunityEndpointsTests(PostgresContainerFixture postgresFixture)
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
    public async Task FullLifecycle_CreateGetJoinPatchLeave()
    {
        using var factory = CreateFactory();
        var creatorUsername = $"creator{Guid.NewGuid():N}"[..15];
        var memberUsername = $"member{Guid.NewGuid():N}"[..15];
        using var creatorClient = await CreateClientWithCompletedProfileAsync(factory, creatorUsername);
        using var memberClient = await CreateClientWithCompletedProfileAsync(factory, memberUsername);

        var communityName = TestNames.UniqueCommunityName();

        // Create
        var createResponse = await creatorClient.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(
                communityName, "A test community", [new CommunityRuleRequest("Be nice", "No insults")],
                TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CommunityResponse>(TestJsonOptions.Default);
        Assert.Equal(1, created!.MemberCount);
        Assert.Single(created.Rules);
        Assert.Equal("Be nice", created.Rules[0].Title);

        // Get
        var getResponse = await creatorClient.GetAsync($"/api/communities/{communityName}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Join (second user)
        var joinResponse = await memberClient.PostAsync($"/api/communities/{communityName}/join", null);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
        var joined = await joinResponse.Content.ReadFromJsonAsync<CommunityResponse>(TestJsonOptions.Default);
        Assert.Equal(2, joined!.MemberCount);

        // Patch as non-mod -> 403
        var forbiddenPatch = await memberClient.PatchAsJsonAsync($"/api/communities/{communityName}",
            new UpdateCommunityRequest("Nope", null));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPatch.StatusCode);

        // Patch as mod -> 200
        var okPatch = await creatorClient.PatchAsJsonAsync($"/api/communities/{communityName}",
            new UpdateCommunityRequest("Updated description", null));
        Assert.Equal(HttpStatusCode.OK, okPatch.StatusCode);
        var patched = await okPatch.Content.ReadFromJsonAsync<CommunityResponse>(TestJsonOptions.Default);
        Assert.Equal("Updated description", patched!.Description);

        // Member leaves fine
        var leaveResponse = await memberClient.DeleteAsync($"/api/communities/{communityName}/leave");
        Assert.Equal(HttpStatusCode.NoContent, leaveResponse.StatusCode);

        // Sole moderator (creator) leaving is rejected
        var soleModLeave = await creatorClient.DeleteAsync($"/api/communities/{communityName}/leave");
        Assert.Equal(HttpStatusCode.Conflict, soleModLeave.StatusCode);
    }

    [Fact]
    public async Task SearchGetAndModerators_WithoutAuth_ReturnAnonymousReads()
    {
        using var factory = CreateFactory();
        using var creatorClient = await CreateClientWithCompletedProfileAsync(factory, $"anon{Guid.NewGuid():N}"[..15]);
        var communityName = TestNames.UniqueCommunityName();
        await creatorClient.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(
            communityName, "Public", null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        using var anonymousClient = factory.CreateClient();

        var searchResponse = await anonymousClient.GetAsync("/api/communities/");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var getResponse = await anonymousClient.GetAsync($"/api/communities/{communityName}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var moderatorsResponse = await anonymousClient.GetAsync($"/api/communities/{communityName}/moderators");
        Assert.Equal(HttpStatusCode.OK, moderatorsResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCommunity_WithIconAndBannerImageUrls_PersistsThemWithoutBlobValidation()
    {
        // Icon/banner are mandatory but, unlike post images, are NOT run through
        // ImageUploadService.ValidateImageAsync at creation — that depends on Azure Blob Storage,
        // which isn't provisioned anywhere in this codebase yet (see PostEndpointsTests'
        // ImageUploadSas_WithoutStorageConfigured_ReturnsServiceUnavailable for where that gap
        // does still block a real blob-backed flow). So creation succeeds here even though
        // storage isn't configured.
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"imgc{Guid.NewGuid():N}"[..15]);
        var communityName = TestNames.UniqueCommunityName();

        var createResponse = await client.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(
                communityName, "desc", null, TestNames.DefaultFlairs,
                IconImageUrl: "https://example.com/icon.png", BannerImageUrl: "https://example.com/banner.png"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CommunityResponse>(TestJsonOptions.Default);
        Assert.Equal("https://example.com/icon.png", created!.IconImageUrl);
        Assert.Equal("https://example.com/banner.png", created.BannerImageUrl);
    }

    [Theory]
    [InlineData("", "https://example.com/banner.png")]
    [InlineData("https://example.com/icon.png", "")]
    public async Task CreateCommunity_MissingIconOrBanner_ReturnsValidationProblem(string icon, string banner)
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"noimg{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();

        var response = await client.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, icon, banner));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateCommunity_InDevelopment_MissingIconAndBannerFallsBackToPlaceholders()
    {
        // ImageUploadField can't produce a real URL locally (blob storage isn't provisioned —
        // see its own comment), so Development relaxes the icon/banner requirement enforced by
        // ValidateImages in CommunityEndpoints and fills in a placeholder instead. Production/
        // Staging/Testing are unaffected — see CreateCommunity_MissingIconOrBanner_ReturnsValidationProblem
        // above, which runs under the "Testing" environment and still expects a 422.
        using var factory = new CustomWebApplicationFactory(postgresFixture.ConnectionString, "Development");
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"devimg{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();

        var response = await client.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, "", ""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CommunityResponse>(TestJsonOptions.Default);
        Assert.False(string.IsNullOrWhiteSpace(created!.IconImageUrl));
        Assert.False(string.IsNullOrWhiteSpace(created.BannerImageUrl));
    }

    [Fact]
    public async Task CreateCommunity_DescriptionOverMaxLength_ReturnsValidationProblem()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"longdesc{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();

        var response = await client.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(
                name, new string('a', 501), null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateCommunity_RuleWithBlankTitle_ReturnsValidationProblem()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"badrule{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();

        var response = await client.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(
                name, null, [new CommunityRuleRequest("", "Some description")],
                TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCommunity_WithRules_ReplacesEntireRuleSet()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"ruleupdate{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();
        await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(
            name, null, [new CommunityRuleRequest("Old rule", "Old description")],
            TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        var patchResponse = await client.PatchAsJsonAsync($"/api/communities/{name}",
            new UpdateCommunityRequest(null, [new CommunityRuleRequest("New rule", "New description")]));

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var patched = await patchResponse.Content.ReadFromJsonAsync<CommunityResponse>(TestJsonOptions.Default);
        Assert.Single(patched!.Rules);
        Assert.Equal("New rule", patched.Rules[0].Title);
    }

    [Fact]
    public async Task CreateCommunity_DuplicateName_ReturnsConflict()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"dupc{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();

        var first = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(
            name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(
            name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetModerators_ReturnsCreatorAsModerator()
    {
        using var factory = CreateFactory();
        var username = $"modlist{Guid.NewGuid():N}"[..15];
        using var client = await CreateClientWithCompletedProfileAsync(factory, username);
        var name = TestNames.UniqueCommunityName();
        await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        var response = await client.GetAsync($"/api/communities/{name}/moderators");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var moderators = await response.Content.ReadFromJsonAsync<List<CommunityModeratorResponse>>(TestJsonOptions.Default);
        Assert.Contains(moderators!, m => m.Username == username);
    }

    [Fact]
    public async Task GetMembers_SearchFiltersByUsernameOrDisplayName_AndAnonymousCanRead()
    {
        using var factory = CreateFactory();
        var creatorUsername = $"membowner{Guid.NewGuid():N}"[..15];
        using var creatorClient = await CreateClientWithCompletedProfileAsync(factory, creatorUsername);
        var name = TestNames.UniqueCommunityName();
        await creatorClient.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        var matchingUsername = $"findme{Guid.NewGuid():N}"[..15];
        using var matchingClient = await CreateClientWithCompletedProfileAsync(factory, matchingUsername);
        await matchingClient.PostAsync($"/api/communities/{name}/join", null);

        var otherUsername = $"other{Guid.NewGuid():N}"[..15];
        using var otherClient = await CreateClientWithCompletedProfileAsync(factory, otherUsername);
        await otherClient.PostAsync($"/api/communities/{name}/join", null);

        using var anonymousClient = factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/communities/{name}/members?search=findme");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<PagedResponse<CommunityMemberResponse>>(TestJsonOptions.Default);
        Assert.Single(members!.Items);
        Assert.Equal(matchingUsername, members.Items[0].Username);
    }

    [Fact]
    public async Task GetMembers_Search_MatchesReverseDirection_WhenSearchTextContainsUsername()
    {
        using var factory = CreateFactory();
        var creatorUsername = $"membrevowner{Guid.NewGuid():N}"[..15];
        using var creatorClient = await CreateClientWithCompletedProfileAsync(factory, creatorUsername);
        var name = TestNames.UniqueCommunityName();
        await creatorClient.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        var matchingUsername = $"nv{Guid.NewGuid():N}"[..8];
        using var matchingClient = await CreateClientWithCompletedProfileAsync(factory, matchingUsername);
        await matchingClient.PostAsync($"/api/communities/{name}/join", null);

        var searchText = $"pre{matchingUsername}post";
        var response = await creatorClient.GetAsync($"/api/communities/{name}/members?search={Uri.EscapeDataString(searchText)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<PagedResponse<CommunityMemberResponse>>(TestJsonOptions.Default);
        Assert.Contains(members!.Items, m => m.Username == matchingUsername);
    }

    [Fact]
    public async Task SearchCommunities_MatchesReverseDirection_WhenSearchTextContainsName()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"browserev{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName(10);
        await client.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        var searchText = $"אב{name}גד";
        var response = await client.GetAsync($"/api/communities/?search={Uri.EscapeDataString(searchText)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<PagedResponse<CommunityResponse>>(TestJsonOptions.Default);
        Assert.Contains(results!.Items, c => c.Name == name);
    }

    [Fact]
    public async Task GetMembers_WithoutSearch_ReturnsAllMembersWithModeratorFirst()
    {
        using var factory = CreateFactory();
        var creatorUsername = $"allmemb{Guid.NewGuid():N}"[..15];
        using var creatorClient = await CreateClientWithCompletedProfileAsync(factory, creatorUsername);
        var name = TestNames.UniqueCommunityName();
        await creatorClient.PostAsJsonAsync("/api/communities/", new CreateCommunityRequest(name, null, null, TestNames.DefaultFlairs, TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        var memberUsername = $"regularm{Guid.NewGuid():N}"[..15];
        using var memberClient = await CreateClientWithCompletedProfileAsync(factory, memberUsername);
        await memberClient.PostAsync($"/api/communities/{name}/join", null);

        var response = await creatorClient.GetAsync($"/api/communities/{name}/members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<PagedResponse<CommunityMemberResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, members!.TotalCount);
        Assert.Equal(CommunityRole.Moderator, members.Items[0].Role);
    }

    [Fact]
    public async Task CreateCommunity_WithNoFlairs_SeedsDefaultFlairs()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"noflairs{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();

        var response = await client.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(name, null, null, [], TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var flairsResponse = await client.GetAsync($"/api/communities/{name}/flairs");
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        Assert.Equal(DefaultFlairs.Names.Count, flairs!.Count);
        Assert.All(flairs, f => Assert.True(f.IsDefault));
    }

    [Fact]
    public async Task CreateCommunity_WithDuplicateFlairNames_ReturnsValidationProblem()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"dupflair{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();

        var response = await client.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(name, null, null,
                [new CreateFlairRequest("Discussion", "#22C55E"), new CreateFlairRequest("Discussion", "#EF4444")],
                TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateCommunity_WithFlairs_PersistsThemAtomically()
    {
        using var factory = CreateFactory();
        using var client = await CreateClientWithCompletedProfileAsync(factory, $"withflairs{Guid.NewGuid():N}"[..15]);
        var name = TestNames.UniqueCommunityName();

        var response = await client.PostAsJsonAsync("/api/communities/",
            new CreateCommunityRequest(name, null, null,
                [new CreateFlairRequest("Discussion", "#22C55E"), new CreateFlairRequest("Question", "#3B82F6")],
                TestNames.DefaultIconUrl, TestNames.DefaultBannerUrl));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var flairsResponse = await client.GetAsync($"/api/communities/{name}/flairs");
        Assert.Equal(HttpStatusCode.OK, flairsResponse.StatusCode);
        var flairs = await flairsResponse.Content.ReadFromJsonAsync<List<FlairResponse>>(TestJsonOptions.Default);
        Assert.Equal(2 + DefaultFlairs.Names.Count, flairs!.Count); // 2 custom + seeded defaults
        Assert.Contains(flairs, f => f.Name == "Discussion" && f.ColorHex == "#22C55E" && !f.IsDefault);
        Assert.Contains(flairs, f => f.Name == "Question" && f.ColorHex == "#3B82F6" && !f.IsDefault);
        Assert.Equal(DefaultFlairs.Names.Count, flairs.Count(f => f.IsDefault));
    }
}
