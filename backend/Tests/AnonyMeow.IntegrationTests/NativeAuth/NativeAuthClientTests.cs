using System.Net;
using System.Text;
using System.Text.Json;
using AnonyMeow.Common.Exceptions;
using AnonyMeow.Common.Options;
using AnonyMeow.Services.NativeAuth;
using Microsoft.Extensions.Options;

namespace AnonyMeow.IntegrationTests.NativeAuth;

// Exercises NativeAuthClient's real HTTP request construction (form-encoded body, client_id,
// challenge_type) and JSON response/error parsing against a fake HttpMessageHandler — no live
// Microsoft tenant involved. Endpoint-level wiring is covered by NativeAuthEndpointsTests;
// orchestration/error-mapping logic is covered by the *FlowServiceTests unit tests.
public class NativeAuthClientTests
{
    private class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }

    private static NativeAuthClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> respond, out FakeHttpMessageHandler handler)
    {
        handler = new FakeHttpMessageHandler(respond);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://anonymeow.ciamlogin.com/anonymeow.onmicrosoft.com/")
        };
        var options = Options.Create(new NativeAuthOptions { BaseUrl = httpClient.BaseAddress.ToString(), ClientId = "test-client-id" });
        return new NativeAuthClient(httpClient, options);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, object body) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SignUpStartAsync_SendsClientIdAndChallengeType_ToCorrectPath()
    {
        var client = CreateClient(_ => JsonResponse(HttpStatusCode.OK, new { continuation_token = "tok-123" }), out var handler);

        var result = await client.SignUpStartAsync("jane@example.com", "P@ssw0rd123");

        Assert.Equal("tok-123", result.ContinuationToken);
        Assert.Equal("none", result.ChallengeType);
        Assert.Contains("client_id=test-client-id", handler.LastRequestBody);
        Assert.Contains("username=jane%40example.com", handler.LastRequestBody);
        Assert.EndsWith("signup/v1.0/start", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SignUpChallengeAsync_ParsesOobChallengeFields()
    {
        var client = CreateClient(_ => JsonResponse(HttpStatusCode.OK, new
        {
            continuation_token = "tok-456",
            challenge_type = "oob",
            challenge_channel = "email",
            challenge_target_label = "j***@example.com",
            code_length = 8
        }), out _);

        var result = await client.SignUpChallengeAsync("tok-123");

        Assert.Equal("oob", result.ChallengeType);
        Assert.Equal("email", result.ChallengeChannel);
        Assert.Equal(8, result.CodeLength);
        Assert.Equal("j***@example.com", result.MaskedTarget);
    }

    [Fact]
    public async Task PostAsync_Throws_NativeAuthApiException_OnErrorResponse()
    {
        var client = CreateClient(_ => JsonResponse(HttpStatusCode.BadRequest, new
        {
            error = "user_already_exists",
            error_description = "An account already exists."
        }), out _);

        var ex = await Assert.ThrowsAsync<NativeAuthApiException>(
            () => client.SignUpStartAsync("jane@example.com", "P@ssw0rd123"));

        Assert.Equal("user_already_exists", ex.Error);
        Assert.Equal("An account already exists.", ex.Message);
    }

    [Fact]
    public async Task PostAsync_Throws_WithRequiredAttributes_OnAttributesRequiredError()
    {
        var client = CreateClient(_ => JsonResponse(HttpStatusCode.BadRequest, new
        {
            error = "attributes_required",
            error_description = "attributes needed",
            required_attributes = new[] { new { name = "displayName" } }
        }), out _);

        var ex = await Assert.ThrowsAsync<NativeAuthApiException>(
            () => client.SignUpContinueWithPasswordAsync("tok-123", "P@ssw0rd123"));

        Assert.Equal("attributes_required", ex.Error);
        Assert.NotNull(ex.RequiredAttributes);
        Assert.Contains("displayName", ex.RequiredAttributes!);
    }

    [Fact]
    public async Task ResetPasswordSubmitAsync_PostsToSubmitEndpoint_WithNewPasswordField()
    {
        var client = CreateClient(_ => JsonResponse(HttpStatusCode.OK, new
        {
            continuation_token = "poll-token",
            poll_interval = 3
        }), out var handler);

        var result = await client.ResetPasswordSubmitAsync("tok-123", "NewP@ssw0rd123");

        Assert.Equal("poll-token", result.ContinuationToken);
        Assert.Equal(3, result.PollIntervalSeconds);
        Assert.Contains("new_password=NewP", handler.LastRequestBody);
        Assert.DoesNotContain("grant_type", handler.LastRequestBody);
        Assert.EndsWith("resetpassword/v1.0/submit", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ResetPasswordPollCompletionAsync_ParsesStatusAndContinuationToken()
    {
        var client = CreateClient(_ => JsonResponse(HttpStatusCode.OK, new
        {
            status = "succeeded",
            continuation_token = "exchange-token"
        }), out var handler);

        var result = await client.ResetPasswordPollCompletionAsync("poll-token");

        Assert.Equal("succeeded", result.Status);
        Assert.Equal("exchange-token", result.ContinuationToken);
        Assert.EndsWith("resetpassword/v1.0/poll_completion", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ExchangeContinuationTokenAsync_ParsesTokenResponse()
    {
        var client = CreateClient(_ => JsonResponse(HttpStatusCode.OK, new
        {
            token_type = "Bearer",
            expires_in = 4141,
            access_token = "access-abc",
            refresh_token = "refresh-abc",
            id_token = "id-abc"
        }), out _);

        var result = await client.ExchangeContinuationTokenAsync("tok-123");

        Assert.Equal("access-abc", result.AccessToken);
        Assert.Equal("refresh-abc", result.RefreshToken);
        Assert.Equal(4141, result.ExpiresIn);
    }
}
