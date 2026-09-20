using System.Diagnostics;

namespace AnonyMeow.Common.Middleware;

// Hard rule: never enrich with the Authorization header, raw JWT, or email — only route/timing
// metadata and the B2C `oid` claim (an opaque identifier, not PII) belong in this scope.
public class RequestLoggingEnrichmentMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingEnrichmentMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var oid = context.User.FindFirst("oid")?.Value;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["Path"] = context.Request.Path.Value,
            ["Method"] = context.Request.Method,
            ["Oid"] = oid
        }))
        {
            await next(context);
            stopwatch.Stop();

            logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
