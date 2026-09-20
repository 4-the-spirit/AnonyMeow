# Cross-cutting concerns

Conventions that apply across every endpoint rather than belonging to one feature area:
exceptions, validation, logging, rate limiting, and a few easy-to-miss gotchas.

## Exception handling

`Common/Exceptions/ApiException.cs` is an abstract base (`StatusCode`, `Title`, `Detail`,
`Extensions`) with ~25 concrete subclasses in the same folder — one per domain error
(`PostNotFoundException`, `SelfVoteException`, `CommunityBannedException`,
`PiiDetectedException`, `PlatformRestrictedException`, `SoleModeratorDemoteException`,
`UsernameConflictException`, `ImageOwnershipMismatchException`, etc.), each fixing its own status
code/title/detail in its constructor. Services throw these directly — there's no
try/catch-and-wrap in endpoint handlers.

`Common/Middleware/GlobalExceptionHandler.cs` (registered via `AddExceptionHandler<T>()` +
`app.UseExceptionHandler()`, first in the pipeline) is the single place that catches them:

- An `ApiException` becomes a `ProblemDetails` with that exception's `StatusCode`/`Title`/`Detail`,
  plus any extra `Extensions` it carries (e.g. `PiiDetectedException`'s detected categories).
- Anything else becomes a generic 500 `ProblemDetails` — `Detail` is only populated with the raw
  `exception.Message` in Development; Production/Staging get a fixed "An unexpected error
  occurred." to avoid leaking internals.
- Every response (both branches) gets a `correlationId` extension
  (`Activity.Current?.Id ?? httpContext.TraceIdentifier`), so a client-reported error can be
  matched back to a specific server-side log entry.
- Only the 500 branch logs at `LogError` — expected `ApiException`s (404s, 409s, validation-style
  409/403s) aren't logged as errors, keeping the error log signal-to-noise ratio meaningful.

## Validation

Request validation is hand-rolled (not FluentValidation) — validators like
`Services/PostValidation/PostRequestValidator.cs` and
`Services/CommentValidation/CommentRequestValidator.cs` return an
`IDictionary<string, string[]>` of field → error messages, and simpler inline checks (e.g.
`CommunityEndpoints`' name-pattern/length/rule checks) build the same shape directly in the
endpoint handler.

`Common/Middleware/ValidationProblemFactory.cs` turns that dictionary into an
`HttpValidationProblemDetails` response with status **422**, not ASP.NET's default 400 for
`TypedResults.ValidationProblem`. This is a deliberate distinction: 400 means "the request itself
is malformed" (bad JSON, wrong route), 422 means "the request was well-formed but fails a semantic
rule" (title too long, invalid poll option, duplicate flair name). Since `TypedResults.ValidationProblem`
has no way to override its status code, `ValidationProblemFactory` builds the identical response
shape manually via `TypedResults.Json(..., statusCode: 422)`.

## Logging

`Common/Middleware/RequestLoggingEnrichmentMiddleware.cs` wraps every request in a
`logger.BeginScope` carrying `Path`, `Method`, and `Oid` (the B2C `oid` claim — an opaque
identifier, not PII) — under a hard rule enforced by a code comment: **never** enrich with the
`Authorization` header, raw JWT, or email. It runs after `UseAuthentication()` (so `oid` is
populated) but before `UseAuthorization()`, so a request that gets 401'd/403'd is still logged.
At the end of the scope it logs one `Information`-level line: method, path, status code, elapsed
milliseconds.

## Rate limiting

Two layers, both partitioned by the same key (`RateLimitPartitionKeys.Resolve`: the authenticated
user's `oid` claim, falling back to remote IP for anonymous callers):

1. **Global baseline** — a single `PartitionedRateLimiter` applied to every request
   (`RateLimitingOptions`, permit/window/queue all configurable).
2. **Named per-action policies**, layered on top via `.RequireRateLimiting("PolicyName")` on
   specific endpoints (`ActionRateLimitOptions`, all fixed-window, `QueueLimit = 0` so excess
   requests are rejected immediately rather than queued):

   | Policy | Default limit | Applied to |
   |---|---|---|
   | `CreatePost` | 5 per 5 min | `POST /api/communities/{name}/posts` |
   | `CreateComment` | 15 per 5 min | `POST /api/posts/{postId}/comments` |
   | `SendMessage` | 30 per 5 min | `POST /api/conversations/{id}/messages` |
   | `Vote` | 60 per 1 min | post/comment vote `PUT` |
   | `Report` | 10 per 5 min | post/comment/message report creation |
   | `ImageUpload` | 30 per 1 day | `POST /api/uploads/images/sas` |

   These are configuration defaults (`ActionRateLimitOptions`), not hardcoded — check
   `appsettings.json`/environment config for the values actually in effect.

Rejections return **429** and are logged as a `Warning` (partition key, method, path) via
`options.OnRejected` — otherwise a rate-limit rejection would be silent, losing visibility into
abuse/brute-force patterns.

## Other gotchas worth knowing about

- **Two independent enum-serialization configs.** `ConfigureHttpJsonOptions` (REST) and
  `AddSignalR().AddJsonProtocol` (the hub) each get their own `JsonStringEnumConverter`
  registration in `Program.cs`. Forgetting one means the same DTO serializes enums differently
  depending on whether it went out over REST or SignalR (e.g. `NotificationResponse.Type` as a
  string via REST but a raw integer via the hub) — worth checking both if you add a new enum to a
  response type that's pushed over SignalR.
- **1 MB Kestrel request body cap** (`MaxRequestBodySize`). Deliberate: no endpoint accepts raw
  file uploads (images go direct-to-Blob via SAS, see [flows.md](flows.md)), so every request body
  is JSON text and 1 MB is generous headroom while still rejecting abusive payloads before model
  binding runs.
- **CORS** is a single configurable origin (`Cors:FrontendOrigin`, default
  `http://localhost:5173`), `AllowAnyHeader`/`AllowAnyMethod`, but **no** `AllowCredentials()` —
  auth is a Bearer header, not cookies, so credentialed CORS was never needed.
- **Security headers** (`SecurityHeadersMiddleware`) lock down `Content-Security-Policy:
  default-src 'none'` plus `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy: strict-origin-when-cross-origin` — safe defaults for a pure JSON API that
  never serves HTML.

## Middleware pipeline order

```
UseExceptionHandler
UseHsts                          (non-Development only)
UseHttpsRedirection
UseResponseCompression
SecurityHeadersMiddleware
UseCors
UseAuthentication
RequestLoggingEnrichmentMiddleware
UseAuthorization
UseRateLimiter
→ endpoint routing / handler execution
```

Order matters here: exception handling wraps everything; response compression sits early so it
applies uniformly to every response, error pages included; authentication has to run before
request logging enrichment (needs the `oid` claim) and before authorization (needs an identity to
authorize); rate limiting runs last so unauthenticated/unauthorized requests still count against
their IP-based partition without reaching a real handler.

## Related docs
- [architecture.md](architecture.md) — where this middleware sits in `Program.cs`'s overall setup.
- [api-surface.md](api-surface.md) — which named rate-limit policy applies to which route.
- [flows.md](flows.md) — flows that produce these errors/rejections in practice (PII block, spam flag, rate limit).
