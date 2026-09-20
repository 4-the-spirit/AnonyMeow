# Authentication & authorization

## Authentication: JWT bearer against Microsoft Entra External ID (CIAM)

All auth is JWT Bearer (`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
in `Program.cs`) — no cookies, no server-side sessions. There is no custom token-parsing code; ASP.NET
Core's built-in JWT Bearer middleware validates every token, per CLAUDE.md's authentication rule.

**Production/Staging**: `Authority`/`Audience` come from the `AzureAdB2COptions` config section
(populated via user-secrets/Azure App Configuration, never committed). The identity provider is
**Microsoft Entra External ID (CIAM)**, not classic Azure AD B2C — B2C stopped accepting new
tenants in May 2025. This matters for the authority URL shape: a CIAM tenant's authority is the
bare `*.ciamlogin.com` instance with **no** `/{domain}/{policy}/v2.0` suffix (e.g.
`https://anonymeow.ciamlogin.com/`), unlike B2C's `{instance}/{domain}/{policy}/v2.0` shape.
Appending the B2C-style suffix here breaks metadata discovery and sign-in — this was an actual
bug, fixed and documented directly in the `Program.cs` comment above the `AddJwtBearer` call.

**Development**: no real CIAM tenant is configured locally, so authority-based metadata discovery
would fail every request. Instead, the JWT Bearer handler is pointed at `DevJwtTokenFactory`'s
fixed issuer/audience/signing key (`Common/Development/DevJwtTokenFactory.cs`), and
`/api/dev/token` (`Endpoints/DevAuthEndpoints.cs`) issues self-signed tokens for local/Postman
testing. Both the token factory and its endpoint are gated behind
`app.Environment.IsDevelopment()` and never reachable in Production/Staging.

**`MapInboundClaims = false`** is set explicitly — without it, `JwtSecurityTokenHandler`'s default
inbound claim map silently renames the token's `oid` claim to a long
`schemas.microsoft.com` URI, breaking every `FindFirst("oid")` lookup
(`ICurrentUserAccessor`, `RequestLoggingEnrichmentMiddleware`) even though the token itself is
valid — the failure mode is silent, not an auth error, which makes it worth calling out here.

**SignalR token relay**: browser WebSocket/SSE transports can't set an `Authorization` header on
the handshake request, so the SignalR JS client sends the bearer token as an `access_token` query
string parameter instead. `JwtBearerEvents.OnMessageReceived` promotes that query param to the
real token, but only for requests under `/hubs` — REST endpoints still require a proper header.

## Authorization: fallback policy + three custom handlers

`Program.cs`'s `AddAuthorizationBuilder()` sets a **fallback policy** — `RequireAuthenticatedUser()`
+ `ProfileCompletionRequirement` — applied to every endpoint that doesn't explicitly opt out. Three
named policies exist beyond that: `AuthenticatedOnly`, `CommunityModerator`, `PlatformAdmin`.

All three custom handlers (`backend/Common/Authorization/*`) share the same shape: a no-op if
`context.User.Identity.IsAuthenticated != true` (letting the framework's own authentication
requirement produce the 401), then resolve the current `AppUser` via `ICurrentUserAccessor` and
check a condition, calling `context.Succeed(requirement)` only if it passes. None of them
explicitly `Fail()` — an unmet requirement just never succeeds, which ASP.NET Core treats as
authorization failure (403) once evaluation completes.

| Handler / Requirement | Applies via | Condition |
|---|---|---|
| `ProfileCompletionHandler` / `ProfileCompletionRequirement` | Fallback policy (every endpoint, unless `AllowAnonymous()` or a stricter named policy is used) | `AppUser.Username` is non-empty — i.e. the user has finished onboarding. |
| `CommunityModeratorHandler` / `CommunityModeratorRequirement` | Resource-based: `IAuthorizationService.AuthorizeAsync(user, community, "CommunityModerator")`, called explicitly in handlers that need it (e.g. `ModerationEndpoints`, `CommunityEndpoints.UpdateCommunityAsync`) — not a route-level `.RequireAuthorization()` attribute, since the target `Community` has to be resolved first. | A `CommunityMembership` row exists for `(community, currentUser)` with `Role == CommunityRole.Moderator`. |
| `PlatformAdminHandler` / `PlatformAdminRequirement` | `.RequireAuthorization("PlatformAdmin")` on the whole `AdminEndpoints` route group | `AppUser.IsPlatformAdmin == true`. |

`AuthenticatedOnly` (no extra requirement beyond `RequireAuthenticatedUser()`) is used where a
route needs to skip `ProfileCompletionRequirement` specifically — namely
`/api/auth/complete-profile` itself (an incomplete-profile user has to be able to call it) and
`/api/users/check-username`.

### Practical effect: what "every endpoint requires auth" actually means

Because the fallback policy is authenticated-by-default, every `Endpoints/*.cs` handler is
implicitly protected unless it calls `.AllowAnonymous()` — used throughout for read-only, publicly
browsable data (post/comment listing and detail, community search/detail/moderators/members,
flair listing, user profile/posts/comments/communities, search, discover/trending). Anything that
mutates state, or reads something scoped to "me," requires at minimum a completed profile.

## Related docs
- [flows.md](flows.md) — the onboarding/profile-completion flow this policy gates, end to end.
- [architecture.md](architecture.md) — where auth setup fits in `Program.cs`'s overall composition.
- [`06-authorization-and-authentication-pipeline.md`](../diagrams/06-authorization-and-authentication-pipeline.md) and the `07`–`09` native-auth sign-up/sign-in/reset-and-refresh flow diagrams in the same folder — the visual counterpart to this doc.
