using System.Net.Http.Headers;
using System.Net.Http.Json;
using AnonyMeow.IntegrationTests.Auth;
using AnonyMeow.IntegrationTests.Fixtures;
using AnonyMeow.Dtos.Users;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace AnonyMeow.IntegrationTests;

// Proves the SignalR query-string access-token auth path (Program.cs's JwtBearerEvents.
// OnMessageReceived) actually works end-to-end. No Authorization header is set on this
// connection — the token travels only via ?access_token=, exactly as a browser client must send
// it, since browsers can't set custom headers on the WebSocket/SSE handshake.
[Collection(IntegrationTestCollection.Name)]
public class NotificationHubConnectionTests(PostgresContainerFixture postgresFixture)
{
    [Fact]
    public async Task Connect_WithAccessTokenQueryString_ReachesConnectedState()
    {
        using var factory = new CustomWebApplicationFactory(postgresFixture.ConnectionString);
        using var client = factory.CreateClient();
        var token = TestJwtTokenFactory.CreateToken(Guid.NewGuid().ToString());

        // The fallback authorization policy requires a completed profile (ProfileCompletionRequirement),
        // so the hub connection needs one too, same as every protected REST endpoint.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var username = $"hubuser{Guid.NewGuid():N}"[..15];
        var completeProfileResponse = await client.PostAsJsonAsync(
            "/api/auth/complete-profile", new CompleteProfileRequest(username, username, "seed"));
        completeProfileResponse.EnsureSuccessStatusCode();

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(client.BaseAddress!, $"/hubs/notifications?access_token={token}"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();
    }
}
