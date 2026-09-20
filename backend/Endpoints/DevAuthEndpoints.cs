using AnonyMeow.Common.Development;
using AnonyMeow.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

// Only mapped when app.Environment.IsDevelopment() (see Program.cs) — lets Postman/local testing
// mint a Bearer token without a real Azure AD B2C tenant. Never available in Production/Staging.
public static class DevAuthEndpoints
{
    public static IEndpointRouteBuilder MapDevAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dev/token", async Task<Results<Ok<DevTokenResponse>, NotFound<string>>> (
                string? oid, string? username, AppDbContext dbContext, CancellationToken cancellationToken) =>
            {
                string subject;
                if (!string.IsNullOrWhiteSpace(username))
                {
                    // The frontend's sign-in-by-username flow: resolve an existing account's oid so
                    // the same dev-token bypass can also act as a (dev-only) "log back in" path.
                    var normalized = username.ToLowerInvariant();
                    var user = await dbContext.Users.SingleOrDefaultAsync(
                        u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
                    if (user is null)
                    {
                        return TypedResults.NotFound("No account found with that username.");
                    }

                    subject = user.B2CObjectId;
                }
                else
                {
                    subject = string.IsNullOrWhiteSpace(oid) ? Guid.NewGuid().ToString() : oid;
                }

                var token = DevJwtTokenFactory.CreateToken(subject);
                return TypedResults.Ok(new DevTokenResponse(subject, token));
            })
            .AllowAnonymous()
            .WithName("GetDevToken");

        return app;
    }
}

public record DevTokenResponse(string Oid, string Token);
