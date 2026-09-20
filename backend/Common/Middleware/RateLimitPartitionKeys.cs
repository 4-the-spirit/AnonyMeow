namespace AnonyMeow.Common.Middleware;

public static class RateLimitPartitionKeys
{
    // Partitions by authenticated user id (oid claim), falling back to remote IP for anonymous
    // callers — the same key resolution the Phase 0 global limiter uses, shared here so every
    // named per-action policy (Phase 8) partitions consistently with it.
    public static string Resolve(HttpContext httpContext) =>
        httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst("oid")?.Value ?? httpContext.User.Identity.Name ?? "authenticated"
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
