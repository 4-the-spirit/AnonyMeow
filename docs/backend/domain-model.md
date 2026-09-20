# Domain model

The 25 EF Core entities in `backend/Domain/*.cs`, each configured by a matching
`IEntityTypeConfiguration<T>` in `backend/Data/Configurations/`, and exposed as a `DbSet<T>` on
`backend/Data/AppDbContext.cs`. Grouped into the same clusters as
[`04-domain-model-core-content.md`](../diagrams/04-domain-model-core-content.md)
and [`05-domain-model-social-moderation.md`](../diagrams/05-domain-model-social-moderation.md)
— this doc is the prose companion to those ERDs, not a restatement of every column.

## Users & Communities

- **`AppUser`** — the platform identity. `B2CObjectId` links it to the OIDC token's `oid` claim
  (see [authentication-and-authorization.md](authentication-and-authorization.md)); `Username` is
  nullable because a user exists (created on first sign-in) before they've completed onboarding —
  see the onboarding flow in [flows.md](flows.md). `Karma`, `FriendListVisibility`,
  `IsPlatformAdmin` round out the profile.
- **`Community`** — a topic forum. Has a generated Postgres full-text-search column
  (`SearchVector`, over `Name` + `Description`) — see "Full-text search" below.
- **`CommunityMembership`** — join entity between `AppUser` and `Community`, carrying a `Role`
  (`CommunityRole.Member` or `.Moderator`). This is the row `CommunityModeratorHandler` queries to
  authorize moderator-only actions.
- **`CommunityRule`** — an ordered (`Order` field) list of house rules per community, edited as a
  whole list rather than individually reordered.
- **`CommunityBan`** — composite-keyed (`CommunityId` + `AppUserId`) record of a ban; presence of
  a row is the ban, not a status flag. Checked by `PostService.CreateAsync` before letting a
  banned user post.

## Content & Engagement

- **`Post`** — belongs to a `Community`, has an `AuthorId`, optional `FlairId`. Content is
  flexible by design: `BodyMarkdown`, `Url`, `Images`, and poll options
  (`PollOption`/`PollVote`) can all be present independently — there's no `PostType`
  discriminator (removed in the `AddPostImagesRemovePostTypeAndImageUrl` migration in favor of
  "a post can carry any combination of body/images/url/poll"). `IsPinned`/`IsLocked`/`IsRemoved`
  are moderation flags set via `ModerationActionService`. Has a generated `SearchVector` over
  `Title` + `BodyMarkdown`.
- **`PostImage`** — one row per image, `Position` preserves display order. Direct-to-Blob upload
  means the URL already points at Azure Blob Storage by the time this row is created (see
  [flows.md](flows.md)'s image upload flow).
- **`PollOption`** / **`PollVote`** — a post's poll choices and one vote per `(PostId, AppUserId)`.
- **`Comment`** — threaded via nullable `ParentCommentId` (self-referencing). Has its own
  generated `SearchVector` over `BodyMarkdown`.
- **`Vote`** — up/down vote on either a `Post` or `Comment`, discriminated by `TargetType`
  (`VoteTargetType.Post`/`.Comment`) + `TargetId` rather than two separate vote tables. `Value` is
  an `sbyte` (+1/-1).
- **`Reaction`** — emoji reaction, same target-type-discriminator shape as `Vote`
  (`ReactionTargetType`), one row per `(TargetType, TargetId, AppUserId, Emoji)` — enforced unique
  per the `ReactionsSingleUniquePerUser` migration.
- **`Flair`** — a community-scoped tag assignable to posts. `IsDefault` marks the fixed starter
  set every new community gets (`Common/DefaultFlairs.cs`) versus custom tags a moderator adds.
- **`SavedPost`** / **`SavedComment`** — simple bookmark join entities, composite-keyed
  (`AppUserId` + target id).

### The target-type discriminator pattern

`Vote`, `Reaction`, `Report`, and `SpamFlag` all follow the same shape: one table, an enum column
(`VoteTargetType`, `ReactionTargetType`, `ReportTargetType`, `SpamFlagTargetType`) that says
whether `TargetId` points at a `Post`, `Comment`, or (for `Report` only) a `Message`
(`ReportTargetType.DirectMessage`), instead of separate `PostVote`/`CommentVote` tables. This
keeps generic operations (cast a vote, count reactions, file a report) as one code path per
concern regardless of what's being acted on, at the cost of the FK not being enforced by the
database — the target's existence is checked in the service layer instead (e.g.
`PostEndpoints`/`CommentEndpoints` both call the same `IVotingService.CastVoteAsync`, just with a
different `VoteTargetType`).

## Moderation, Safety & Messaging

- **`Report`** — a user flagging a `Post`/`Comment`/`Message` for review, with a required
  `Category` (`ReportReasonCategory`: Spam, Harassment, HateSpeech, Violence, Misinformation,
  Nsfw, Other) and optional free-text `Reason`. `Status` moves `Open → ActionTaken`/`Dismissed`
  once a moderator resolves it (`ReportService.ResolveAsync`).
- **`ModerationAction`** — the audit trail. One row per mod/admin action
  (`ModerationActionType`: Pin, Unpin, Lock, Unlock, Remove, Ban, Unban, ReportResolve,
  PlatformRestrict, PlatformSuspend, PlatformRestrictionLifted, ModeratorPromoted,
  ModeratorDemoted), written by `ModerationActionService` alongside every action it performs.
  `CommunityId` is nullable because platform-level actions (restrict/suspend) aren't
  community-scoped.
- **`SpamFlag`** — an automated flag from the spam-detection heuristics (`SpamFlagReason`:
  RateLimitExceeded, DuplicateContent, LinkSpam), linked to the `Report` it filed
  (`ReportId`) so a spam flag surfaces in the same moderation queue as user reports.
- **`PlatformRestriction`** — an admin-issued, platform-wide restriction (as opposed to
  `CommunityBan`'s single-community scope). `EndAtUtc` is nullable (indefinite); a restriction
  only stops applying when an admin explicitly sets `Status = Lifted` — an expired-but-unlifted
  `EndAtUtc` still blocks (see `PostService.CreateAsync`'s check).
- **`PiiDetectionLog`** — one row per PII match found in submitted content, logging only a
  redacted category label (`MatchedCategory`, e.g. `"Email"`) and whether it blocked the
  submission (`WasBlocked`) — **never** the matched text itself.
- **`Friendship`** — composite-keyed (`RequesterId` + `AddresseeId`) with a `FriendshipStatus`
  state machine (request → accept/decline).
- **`UserBlock`** — one-directional block (`BlockerId` blocks `BlockedId`).
- **`Conversation`** — a DM thread between exactly two participants (`ParticipantAId`/`B`). Pin
  and delete are per-participant, not global, so they're modeled as two independent nullable
  timestamp pairs (`PinnedByAAtUtc`/`PinnedByBAtUtc`, `DeletedByAAtUtc`/`DeletedByBAtUtc`) rather
  than a single shared flag — `Conversation.IsPinnedBy(userId)`/`IsDeletedBy(userId)` resolve
  which side of the pair applies.
- **`Message`** — belongs to a `Conversation`, optional `ReplyToMessageId` for threaded replies.
- **`Notification`** — a generic notification row (`NotificationType`: Reply, Mention, ModAction,
  FriendRequest) with a `SourceType`/`SourceId` pair pointing at whatever triggered it, plus
  `PreviewText` for display. Written and pushed together by `NotificationDispatcher` — see
  [flows.md](flows.md).

## EF Core conventions

- **One configuration class per entity** in `Data/Configurations/`, applied via
  `modelBuilder.ApplyConfigurationsFromAssembly(...)` in `AppDbContext.OnModelCreating` — keeps
  Fluent API config (keys, indexes, required fields, max lengths) out of `AppDbContext` itself and
  colocated per entity.
- **Postgres-only full-text search.** `Post.SearchVector`, `Comment.SearchVector`, and
  `Community.SearchVector` are `NpgsqlTsVector` generated columns (`HasGeneratedTsVectorColumn`,
  GIN-indexed), configured conditionally in `AppDbContext.OnModelCreating`: only when
  `Database.IsNpgsql()` is true. Under the EF Core **InMemory** provider (used by
  `AnonyMeow.UnitTests`), those columns are explicitly `.Ignore(...)`'d instead, since
  `NpgsqlTsVector` is an Npgsql-only CLR type the InMemory provider can't map. This is why unit
  tests never assert against full-text search — that's covered by the integration test suite
  against real Postgres instead (see [testing.md](testing.md)).
- **`AppDbContextFactory`** (`Data/AppDbContextFactory.cs`) is a design-time `IDesignTimeDbContextFactory<AppDbContext>`
  used by `dotnet ef migrations add/database update` tooling — it doesn't run at application
  startup, only when the EF Core CLI needs to construct a context outside of DI (e.g. no running
  `Program.cs` host to resolve `IConfiguration` from).

## Migration history as evolution narrative

`Data/Migrations/` (19 migrations as of `SeedProtestStatementInfoFlairs`) roughly tells the
feature build order: `AddAppUser` → `AddCommunities` → `AddPosts` → `AddCommentsAndVotes` →
`AddModeration` → `AddFlairsAndReactions` → `AddNotifications` → `AddUserBlocks` →
`AddConversationsAndMessages` → `AddPiiDetectionLogs` → `AddCommunityImages` →
`AddSavedPostsSpamFlagsPlatformRestrictions` → `AddFullTextSearch` →
`AddPostImagesRemovePostTypeAndImageUrl` → `AddTextLengthLimits` →
`AddMessageReplyAndUserPresence` → `AddConversationPinAndDelete` → `AddSavedComments` →
`ReactionsSingleUniquePerUser` → `AddReportCategory` → `AddDefaultFlairFlag` →
`RestructureCommunityRulesAndRequireImages` → `RecolorDefaultFlairs` →
`SeedProtestStatementInfoFlairs`. Two migrations are worth knowing about if you're reading older
code or tests: `AddPostImagesRemovePostTypeAndImageUrl` removed a `Post.Type` discriminator column
in favor of the current "independent body/images/url/poll" model, and
`ReactionsSingleUniquePerUser` added the uniqueness constraint described above.

Per CLAUDE.md, all schema changes go through EF Core migrations — never hand-edit the schema —
and migrations touching drops/renames should be reviewed for destructive operations before
applying.

## Related docs
- [architecture.md](architecture.md) — how `Data/`, `Domain/`, and `Dtos/` relate to the rest of the backend.
- [flows.md](flows.md) — how these entities get created/mutated end-to-end (post creation, moderation, messaging).
- [`04-domain-model-core-content.md`](../diagrams/04-domain-model-core-content.md), [`05-domain-model-social-moderation.md`](../diagrams/05-domain-model-social-moderation.md) — the visual ERDs this doc complements.
