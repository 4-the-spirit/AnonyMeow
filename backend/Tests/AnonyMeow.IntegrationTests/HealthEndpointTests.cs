using System.Net;
using AnonyMeow.IntegrationTests.Fixtures;

namespace AnonyMeow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class HealthEndpointTests(PostgresContainerFixture postgresFixture)
{
    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory(postgresFixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_IncludesSecurityHeaders()
    {
        using var factory = new CustomWebApplicationFactory(postgresFixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }
}
