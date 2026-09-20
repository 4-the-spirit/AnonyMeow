namespace AnonyMeow.Common.Middleware;

// This is a JSON API with no HTML responses, so a locked-down CSP is safe and cheap defense in
// depth. HSTS is configured separately via app.UseHsts() (Program.cs), since ASP.NET Core's
// convention is to gate that behind non-Development environments.
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Content-Security-Policy"] = "default-src 'none'";
            return Task.CompletedTask;
        });

        await next(context);
    }
}
