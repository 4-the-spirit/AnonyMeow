# Testing

Run everything with `dotnet test "AnonyMeow.sln"` from `backend/` (per CLAUDE.md, both unit and
integration tests must pass after any backend change).

## `AnonyMeow.UnitTests` — business logic in isolation

Mirrors `Services/` 1:1: one test class per service
(`Services/PostServiceTests.cs`, `CommentServiceTests.cs`, `VotingServiceTests.cs`,
`ModerationActionServiceTests.cs`, `NotificationDispatcherTests.cs`, `FriendshipServiceTests.cs`,
`FlairServiceTests.cs`, `SearchServiceTests.cs` — most of the ~25 services in
`Services/` have a matching test file), plus subfolders for the multi-strategy areas:
`Services/PiiDetection/` (one test class per detector + the composite),
`Services/SpamDetection/` (one per heuristic + the composite + `SpamFlaggingServiceTests`),
`Services/Ranking/SortStrategyTests.cs`, `Services/PostValidation/PostRequestValidatorTests.cs`,
`Services/CommentValidation/CommentRequestValidatorTests.cs`. Plus
`Authorization/CommunityModeratorHandlerTests.cs` and `ProfileCompletionHandlerTests.cs`
(the two data/resource-dependent authorization handlers — `PlatformAdminHandler` is simple enough
it isn't separately unit-tested), and `Common/GlobalExceptionHandlerTests.cs`.

These run against EF Core's **InMemory** provider, not real Postgres — which is why
`AppDbContext.OnModelCreating` explicitly `.Ignore()`s the `NpgsqlTsVector` search columns under
that provider (see [domain-model.md](domain-model.md)). Anything that depends on Postgres-specific
behavior (full-text search, generated columns) is out of scope for this suite by construction —
that's what the integration suite is for.

## `AnonyMeow.IntegrationTests` — full request pipeline against real Postgres

One test class per endpoint group (`PostEndpointsTests.cs`, `CommunityEndpointsTests.cs`,
`CommentAndVotingEndpointsTests.cs`, `ModerationEndpointsTests.cs`, `AdminEndpointsTests.cs`,
`FriendshipEndpointsTests.cs`, `BlockEndpointsTests.cs`, `ConversationAndMessageEndpointsTests.cs`,
`NotificationEndpointsTests.cs`, `NotificationHubConnectionTests.cs`, `SavedPostAndFeedEndpointsTests.cs`,
`SavedCommentEndpointsTests.cs`, `SearchEndpointsTests.cs`, `DiscoverEndpointsTests.cs`,
`FlairAndReactionEndpointsTests.cs`, `SpamDetectionAndRateLimitEndpointsTests.cs`,
`ProfileEndpointsTests.cs`, `UserEndpointsTests.cs`, `HealthEndpointTests.cs`), plus supporting
infrastructure:

- **`CustomWebApplicationFactory`** — a `WebApplicationFactory<Program>` that swaps real CIAM/B2C
  token validation for a test-only signing key (`PostConfigure<JwtBearerOptions>`), so tests mint
  their own tokens via `Auth/TestJwtTokenFactory.cs` (mirrors `Common/Development/DevJwtTokenFactory.cs`)
  without needing a live CIAM tenant, and points `ConnectionStrings:Default` at the fixture's
  connection string.
- **`Fixtures/PostgresContainerFixture`** — a **Testcontainers**-managed real `postgres:16-alpine`
  container, spun up once and migrated (`Database.MigrateAsync()`) via `IAsyncLifetime`, shared
  across the collection (`Fixtures/IntegrationTestCollection.cs`) rather than one container per
  test class — matches production behavior for things InMemory can't (full-text search generated
  columns, real constraint enforcement), per CLAUDE.md's rule that integration tests use a real
  Postgres instance.
- **`TestJsonOptions.cs`** — shared `JsonSerializerOptions` (matching the app's own
  `JsonStringEnumConverter` setup) for deserializing test HTTP responses.
- **`Fixtures/TestNames.cs`** — shared naming helpers to keep generated test usernames/community
  names collision-free within a shared container.

Requires Docker running locally (Testcontainers needs a container runtime) — this is also why
CLAUDE.md calls out `scripts/test-like-ci.ps1` for OS-dependent bugs: local dev is often Windows,
but CI runs on `ubuntu-latest`, so anything touching URL/path parsing or culture-sensitive logic
should be verified in that Linux container before pushing.

## A note on the `Tests/` folder layout

`backend/Tests/` currently has **four** project folders, but only two are live:
`AnonyMeow.UnitTests` and `AnonyMeow.IntegrationTests` (above) contain all the actual test code.
`AnonyMeow.Backend.UnitTests` and `AnonyMeow.Backend.IntegrationTests` exist as `.csproj` files
with no `.cs` source — leftover scaffolding, most likely from the same repo-flattening rename that
turned `AnonyMeow - backend/` into `backend/` (visible in this repo's git status). They aren't
referenced by `AnonyMeow.sln` in any way that adds real test coverage. Worth a deliberate decision
(remove them, or repurpose them) rather than leaving them as ambient clutter — flagged here as an
observation, not addressed by this documentation change.

## Related docs
- [domain-model.md](domain-model.md) — the InMemory-vs-Npgsql provider split these tests are built around.
- [cross-cutting-concerns.md](cross-cutting-concerns.md) — GlobalExceptionHandler and validation, both directly unit-tested.
