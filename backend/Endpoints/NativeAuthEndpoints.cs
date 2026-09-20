using System.IdentityModel.Tokens.Jwt;
using AnonyMeow.Common.Middleware;
using AnonyMeow.Dtos.NativeAuth;
using AnonyMeow.Services.NativeAuth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AnonyMeow.Endpoints;

// Proxies the frontend to Microsoft Entra External ID's native authentication REST API (see
// Services/NativeAuth/) — that API doesn't support CORS, so it can't be called directly from the
// browser (see docs/azure-ciam-setup.md). Every route here is unauthenticated by nature (that's
// the point) and rate-limited against credential-guessing abuse.
public static class NativeAuthEndpoints
{
    public static IEndpointRouteBuilder MapNativeAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth/native").AllowAnonymous();

        group.MapPost("/signup/start", SignUpStartAsync)
            .WithName("NativeSignUpStart")
            .RequireRateLimiting("SignUpStart");

        group.MapPost("/signup/verify-email", SignUpVerifyEmailAsync)
            .WithName("NativeSignUpVerifyEmail")
            .RequireRateLimiting("SignUpVerify");

        group.MapPost("/signin/start", SignInStartAsync)
            .WithName("NativeSignInStart")
            .RequireRateLimiting("SignInStart");

        group.MapPost("/refresh", RefreshAsync)
            .WithName("NativeAuthRefresh")
            .RequireRateLimiting("RefreshToken");

        group.MapPost("/password-reset/start", PasswordResetStartAsync)
            .WithName("NativePasswordResetStart")
            .RequireRateLimiting("PasswordReset");

        group.MapPost("/password-reset/verify", PasswordResetVerifyAsync)
            .WithName("NativePasswordResetVerify")
            .RequireRateLimiting("PasswordReset");

        group.MapPost("/password-reset/complete", PasswordResetCompleteAsync)
            .WithName("NativePasswordResetComplete")
            .RequireRateLimiting("PasswordReset");

        return app;
    }

    private static async Task<Results<Ok<SignUpStartResponse>, JsonHttpResult<HttpValidationProblemDetails>>> SignUpStartAsync(
        SignUpStartRequest request, ISignUpFlowService signUpFlowService, CancellationToken cancellationToken)
    {
        if (ValidateEmailAndPassword(request.Email, request.Password) is { } errors)
        {
            return ValidationProblemFactory.Create(errors);
        }

        var result = await signUpFlowService.StartAsync(request.Email, request.Password, cancellationToken);
        return TypedResults.Ok(result.OtpChallenge is { } challenge
            ? new SignUpStartResponse("verifyEmail", ToDto(challenge), null)
            : new SignUpStartResponse("completed", null, ToDto(result.Tokens!)));
    }

    private static async Task<Ok<AuthTokensResponse>> SignUpVerifyEmailAsync(
        SignUpVerifyEmailRequest request, ISignUpFlowService signUpFlowService, CancellationToken cancellationToken)
    {
        var tokens = await signUpFlowService.VerifyEmailAsync(
            request.ContinuationToken, request.Code, cancellationToken);
        return TypedResults.Ok(ToDto(tokens));
    }

    private static async Task<Results<Ok<AuthTokensResponse>, JsonHttpResult<HttpValidationProblemDetails>>> SignInStartAsync(
        SignInStartRequest request, ISignInFlowService signInFlowService, CancellationToken cancellationToken)
    {
        if (ValidateEmailAndPassword(request.Email, request.Password) is { } errors)
        {
            return ValidationProblemFactory.Create(errors);
        }

        var tokens = await signInFlowService.StartAsync(request.Email, request.Password, cancellationToken);
        return TypedResults.Ok(ToDto(tokens));
    }

    private static async Task<Ok<AuthTokensResponse>> RefreshAsync(
        RefreshSessionRequest request, INativeAuthClient client, CancellationToken cancellationToken)
    {
        var tokens = await client.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        return TypedResults.Ok(ToDto(tokens));
    }

    private static async Task<Ok<OtpChallengeResponse>> PasswordResetStartAsync(
        PasswordResetStartRequest request, IPasswordResetFlowService passwordResetFlowService, CancellationToken cancellationToken)
    {
        var challenge = await passwordResetFlowService.StartAsync(request.Email, cancellationToken);
        return TypedResults.Ok(ToDto(challenge));
    }

    private static async Task<Ok<PasswordResetVerifyResponse>> PasswordResetVerifyAsync(
        PasswordResetVerifyRequest request, IPasswordResetFlowService passwordResetFlowService, CancellationToken cancellationToken)
    {
        var continuationToken = await passwordResetFlowService.VerifyCodeAsync(
            request.ContinuationToken, request.Code, cancellationToken);
        return TypedResults.Ok(new PasswordResetVerifyResponse(continuationToken));
    }

    private static async Task<Ok<AuthTokensResponse>> PasswordResetCompleteAsync(
        PasswordResetCompleteRequest request, IPasswordResetFlowService passwordResetFlowService, CancellationToken cancellationToken)
    {
        var tokens = await passwordResetFlowService.CompleteAsync(
            request.ContinuationToken, request.NewPassword, cancellationToken);
        return TypedResults.Ok(ToDto(tokens));
    }

    private static Dictionary<string, string[]>? ValidateEmailAndPassword(string email, string password)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            errors["email"] = ["Enter a valid email address."];
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errors["password"] = ["Password is required."];
        }

        return errors.Count > 0 ? errors : null;
    }

    private static OtpChallengeResponse ToDto(NativeAuthStepResult step) =>
        new(step.ContinuationToken!, step.CodeLength, step.MaskedTarget);

    // The id_token's oid claim is read here (not re-validated — it's trusted as coming directly
    // from Microsoft over a server-to-server HTTPS call) purely so the frontend receives it
    // pre-extracted, the same way it already gets `oid` from the dev-token bypass, without ever
    // needing to decode a JWT client-side.
    private static AuthTokensResponse ToDto(NativeAuthTokenResult tokens)
    {
        var idToken = new JwtSecurityTokenHandler().ReadJwtToken(tokens.IdToken);
        var oid = idToken.Claims.First(c => c.Type == "oid").Value;
        return new AuthTokensResponse(tokens.AccessToken, tokens.IdToken, tokens.RefreshToken, oid, tokens.ExpiresIn);
    }
}
