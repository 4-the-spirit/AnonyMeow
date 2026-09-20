# Backend architecture

How `backend/` is laid out, how the pieces depend on each other, and the conventions `Program.cs`
establishes as the composition root.

## Layers

```
backend/
  Domain/            Plain EF Core entity classes (POCOs) + Enums/ — no behavior, no attributes
  Data/               AppDbContext, Configurations/ (Fluent API), Migrations/
  Dtos/               Request/response records, one subfolder per feature area
  Endpoints/          Minimal API route registration, one static class per feature
  Services/           Business logic, one interface + implementation per concern
  Common/
    Authorization/    Custom IAuthorizationHandler/IAuthorizationRequirement pairs
    Development/      Dev-only JWT issuer
    Exceptions/       ApiException and ~25 concrete subclasses
    Middleware/        Exception handling, request logging, security headers, rate-limit keys
    Options/           Strongly-typed config classes (bound from appsettings/user-secrets)
  Hubs/               SignalR hub (NotificationHub)
  Program.cs          Composition root: DI registration, middleware pipeline, route mounting
```

Dependency direction is one-way: **Endpoints → Services → Data/Domain**, with **Dtos** as the
wire boundary in both directions. Endpoint handlers never touch `Domain` entities directly in
their return types — every response goes through a Dto's `FromEntity(...)` factory
(`backend/Endpoints/PostResponseAssembly.cs`, `CommentResponseAssembly.cs`, or a static method on
the Dto itself, e.g. `PostResponse.FromEntity`). There is no separate repository layer — services
depend on `AppDbContext` directly via constructor injection; `AppDbContext` itself is the
abstraction service tests substitute (EF Core's InMemory provider in unit tests, a real
Testcontainers-backed Postgres in integration tests — see [testing.md](testing.md)).

## Endpoints: one static class per feature

Each file in `Endpoints/` is a `static class` with a `MapXEndpoints(this IEndpointRouteBuilder app)`
extension method, called once from `Program.cs`. Handlers are private static methods bound via
Minimal API's parameter injection (services, route/query params, and `CancellationToken` are all
resolved per-request — no `[FromServices]` boilerplate needed). Some feature areas share a route
prefix via `app.MapGroup(prefix)` (e.g. `/api/communities`, `/api/admin`, `/api/conversations`,
`/api/notifications`) so a group-level policy (`RequireAuthorization("PlatformAdmin")` on
`AdminEndpoints`) or `AllowAnonymous()` can be layered once; most feature areas instead call
`app.MapGet/Post/...` directly with a fully-qualified path. See
[api-surface.md](api-surface.md) for the full route table.

Two files — `PostResponseAssembly.cs` and `CommentResponseAssembly.cs` — aren't endpoint groups;
they're response-assembly helpers co-located with the endpoints that use them, because building a
`PostResponse`/`CommentResponse` means joining several services (score, comment count, flair,
reactions, viewer's own vote) that would otherwise be repeated inline in every handler that
returns a post or comment.

## Program.cs as the composition root

`Program.cs` is intentionally flat and linear — it's the one place that wires everything
together, in this order:

1. **Kestrel limits** — `MaxRequestBodySize = 1_000_000` (1 MB). No endpoint accepts raw file
   uploads (images go direct-to-Blob via SAS, see [flows.md](flows.md)), so every request body is
   JSON text and 1 MB is generous headroom while still rejecting oversized payloads before model
   binding.
2. **JSON options** — a `JsonStringEnumConverter` registered via `ConfigureHttpJsonOptions` so
   request/response bodies serialize enums as strings (`"Hot"`, not `0`). SignalR's JSON hub
   protocol has its **own**, separately-configured serializer (`AddSignalR().AddJsonProtocol`) —
   an easy inconsistency to miss if you only touch one of the two.
3. **`AppDbContext`** registered against Npgsql, connection string from configuration.
4. **Exception handling** — `AddProblemDetails()` + `AddExceptionHandler<GlobalExceptionHandler>()`.
5. **Service registrations** — every service interface/implementation pair as `Scoped`, plus:
   - **Keyed DI** for ranking: `AddKeyedScoped<ISortStrategy, HotSortStrategy>(SortOrder.Hot)` etc.,
     one registration per `SortOrder` enum value, resolved later by `RankingService` via
     `GetRequiredKeyedService<ISortStrategy>(sortOrder)`.
   - **Composite pattern, twice**: every `IPiiDetector` and every `ISpamHeuristic` is registered
     individually; `CompositePiiDetectionService`/`CompositeSpamDetectionService` take
     `IEnumerable<IPiiDetector>`/`IEnumerable<ISpamHeuristic>` and run all of them. Adding a new
     detector/heuristic is a one-line registration, no changes to the composite.
   - Every `IAuthorizationHandler` (`ProfileCompletionHandler`, `CommunityModeratorHandler`,
     `PlatformAdminHandler`) is registered the same way as a service — ASP.NET Core's
     authorization system resolves all registered handlers per policy evaluation.
6. **Options pattern** — `Configure<TOptions>(section)` for `AzureAdB2COptions`,
   `RateLimitingOptions`, `BlobStorageOptions`, `ReactionOptions`, `PostImageOptions`,
   `PiiDetectionOptions`, `SpamDetectionOptions`. Config differences between environments are
   meant to flow through these sections (via user-secrets locally, Azure App
   Configuration/Key Vault in the cloud), not code branches — per CLAUDE.md's Azure conventions.
7. **AuthN/AuthZ setup** — see [authentication-and-authorization.md](authentication-and-authorization.md).
8. **Rate limiting** — a global partitioned fixed-window limiter plus named per-action policies;
   see [cross-cutting-concerns.md](cross-cutting-concerns.md).
9. **SignalR** with its own JSON protocol config (point 2 above).
10. **CORS** — single configurable frontend origin, no credentials (Bearer auth, not cookies).

Then the pipeline is built and the middleware order is fixed (`app.Use...` calls) before every
`MapXEndpoints()` call runs and the SignalR hub is mapped at `/hubs/notifications`. Middleware
order is covered in [cross-cutting-concerns.md](cross-cutting-concerns.md).

## Design patterns in active use

| Pattern | Where | Why |
|---|---|---|
| **Strategy** | `Services/Ranking/Strategies/*` (`ISortStrategy`, one class per `SortOrder`) | Each sort order (Hot/New/Top/Controversial/Trending/Pinned) is an independent, swappable `IQueryable<Post>`/`IQueryable<Comment>` transform, resolved by keyed DI rather than a big `switch`. |
| **Composite** | `Services/PiiDetection/*` (`IPiiDetector`), `Services/SpamDetection/*` (`ISpamHeuristic`) | Both run "every registered check, aggregate the results" — adding a new detector/heuristic never touches the composite or the pipeline that calls it. |
| **Pipeline** | `Services/ContentSubmission/ContentSubmissionPipeline` | A single `EvaluateAsync` entry point that content-creating services (post/comment/message) call before persisting, decoupling "is this text allowed" from the entities that submit text. |
| **Options** | `Common/Options/*` + `builder.Services.Configure<T>(section)` | Strongly-typed, environment-driven config instead of `IConfiguration["Key"]` string lookups scattered through services. |
| **Fallback + named authorization policies** | `Program.cs` `AddAuthorizationBuilder()` | A default policy applies to every endpoint (auth + profile completion) unless a handler opts out (`AllowAnonymous()`) or opts into a stricter named policy (`CommunityModerator`, `PlatformAdmin`). |

There is deliberately **no** Repository/Unit-of-Work layer on top of EF Core — `AppDbContext` is
injected directly into services, which is EF Core's own repository/unit-of-work abstraction. No
MediatR/CQRS — endpoint handlers call services directly; the codebase is small enough that an
extra dispatch layer would add indirection without a matching benefit.

## Related docs
- [domain-model.md](domain-model.md) — the entities `AppDbContext` exposes and how they relate.
- [authentication-and-authorization.md](authentication-and-authorization.md) — the auth setup summarized in point 7 above.
- [api-surface.md](api-surface.md) — the full endpoint route table.
- [cross-cutting-concerns.md](cross-cutting-concerns.md) — middleware pipeline, exceptions, validation, rate limiting, logging.
- [`03-backend-layered-architecture-and-pipeline.md`](../diagrams/03-backend-layered-architecture-and-pipeline.md) — the visual counterpart to this doc (middleware pipeline order + layering).
