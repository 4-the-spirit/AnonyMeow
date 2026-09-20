using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Dtos.Blocks;
using AnonyMeow.Dtos.Users;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class BlockEndpointsTests(PostgresContainerFixture postgresFixture)
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

    [Fact]
    public async Task BlockAndUnblock_HappyPath()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "blocka");
        var (_, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "blockb");

        var blockResponse = await a.PostAsync($"/api/users/{bUsername}/block", null);
        Assert.Equal(HttpStatusCode.Created, blockResponse.StatusCode);
        var block = await blockResponse.Content.ReadFromJsonAsync<BlockResponse>(TestJsonOptions.Default);
        Assert.Equal(bUsername, block!.BlockedUsername);

        var unblockResponse = await a.DeleteAsync($"/api/users/{bUsername}/block");
        Assert.Equal(HttpStatusCode.NoContent, unblockResponse.StatusCode);
    }

    [Fact]
    public async Task Block_Self_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        var (a, aUsername) = await CreateClientWithCompletedProfileAsync(factory, "blockself");

        var response = await a.PostAsync($"/api/users/{aUsername}/block", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Block_UnknownUsername_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "blocknf");

        var response = await a.PostAsync("/api/users/does-not-exist-user/block", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Block_CalledTwice_IsIdempotent_ReturnsCreatedBothTimes()
    {
        using var factory = CreateFactory();
        var (a, _) = await CreateClientWithCompletedProfileAsync(factory, "blockidema");
        var (_, bUsername) = await CreateClientWithCompletedProfileAsync(factory, "blockidemb");

        var first = await a.PostAsync($"/api/users/{bUsername}/block", null);
        var second = await a.PostAsync($"/api/users/{bUsername}/block", null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }
}
