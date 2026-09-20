using AnonyMeow.Data;

namespace AnonyMeow.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

                bool databaseReachable;
                try
                {
                    databaseReachable = await db.Database.CanConnectAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    databaseReachable = false;
                }

                return databaseReachable
                    ? Results.Ok(new { status = "ok", database = "ok" })
                    : Results.Json(
                        new { status = "unhealthy", database = "unreachable" },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .AllowAnonymous()
            .WithName("GetHealth");

        return app;
    }
}
