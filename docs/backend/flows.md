# Key backend flows

Narrative, step-by-step walkthroughs of the flows that cut across multiple services — the kind of
thing that's hard to see from any single file. Static structure is covered in
[architecture.md](architecture.md)/[domain-model.md](domain-model.md); the CIAM sign-in sequence
itself has its own diagrams ([`06-authorization-and-authentication-pipeline.md`](../diagrams/06-authorization-and-authentication-pipeline.md)
through `09-auth-password-reset-and-refresh-flow.md`) and isn't repeated here.

## 1. Onboarding / profile completion

1. A user signs in via CIAM's native-auth email/password flow (custom form + OTP, not the MSAL
   popup this doc used to describe — see `frontend/src/features/auth/nativeAuthAdapter.ts`) and
   calls a protected endpoint with a valid JWT.
   `AppUser` doesn't exist for a first-time signer-in until something creates it — `ICurrentUserAccessor`
   resolves (and implicitly provisions) the `AppUser` row from the token's `oid`/`B2CObjectId`, with
   `Username` left `null`.
2. The global fallback authorization policy (`ProfileCompletionRequirement`) fails for every
   endpoint except those marked `AllowAnonymous()` or `AuthenticatedOnly` — see
   [authentication-and-authorization.md](authentication-and-authorization.md) — until `Username`
   is set. This is what forces a new sign-in through the onboarding form before touching the rest
   of the API.
3. Frontend calls `GET /api/users/check-username?value=...` (policy: `AuthenticatedOnly`, so it
   works even with an incomplete profile) to validate availability live as the user types.
4. Frontend submits `POST /api/auth/complete-profile` (also `AuthenticatedOnly`) with username,
   display name, avatar seed. `UserEndpoints.CompleteProfileAsync` validates format/presence
   inline, then calls `IUsernameReservationService.ReserveAsync` — which re-checks availability
   (race-safe against two concurrent claims of the same name) and throws
   `UsernameConflictException` if it lost the race — before setting `DisplayName`/`AvatarSeed` and
   saving.
5. From this point on, `ProfileCompletionRequirement` succeeds and every other endpoint becomes
   reachable.

## 2. Post creation with content safety (PII + spam)

`PostService.CreateAsync` (`Endpoints/PostEndpoints.cs` → `PostService.cs`), in order:

1. **Flair validation** — `IFlairService.EnsureValidForCommunityAsync` confirms the chosen flair
   exists and belongs to this community (a flair is mandatory on every post).
2. **Ban check** — a `CommunityBan` row for `(community, author)` throws `CommunityBannedException`.
3. **Platform restriction check** — an active `PlatformRestriction` (not yet lifted, and either no
   `EndAtUtc` or still in the future) throws `PlatformRestrictedException`. This is separate from
   the community ban check: a platform restriction blocks posting *everywhere*, not just one
   community.
4. **PII detection** — title + body are concatenated and passed to
   `IContentSubmissionPipeline.EvaluateAsync`. Internally: `CompositePiiDetectionService` runs
   every registered `IPiiDetector` (`EmailPatternDetector`, `PhoneNumberDetector`,
   `AddressHeuristicDetector`) against the text; each detector's enforcement mode
   (`Block`/`LogOnly`/`Disabled`, from `PiiDetectionOptions`, re-read per call so ops can change it
   without a redeploy) decides whether a match blocks the submission. Every match — blocked or
   not — is written to `PiiDetectionLog` (redacted category label only, never the raw matched
   text). If any match is `WasBlocked`, `PostService` throws `PiiDetectedException` **before any
   `Post` row is written** — this is a hard block, not a flag-for-review.
5. **Image validation** — for each submitted image URL, `IImageUploadService.ValidateImageAsync`
   checks the blob path starts with `{authorId}/` (proves the caller actually holds a SAS for that
   blob — see the image upload flow below), the blob's `ContentLength` is under
   `PostImageOptions.MaxImageSizeBytes`, and a file-signature probe of the first 12 bytes matches
   a known image format (JPEG/PNG/GIF/WEBP) — deleting the blob and throwing on any failure.
6. The `Post` (+ `PostImage`/`PollOption` rows) is created and saved.
7. **Spam detection runs *after* the save**, unlike PII — `SpamFlaggingService.FlagIfSpamAsync`
   needs the post's id to file a `Report` against it if flagged. `CompositeSpamDetectionService`
   runs every `ISpamHeuristic` (`PostingFrequencyHeuristic`, `DuplicateContentHeuristic`,
   `LinkSpamHeuristic`); any hits create a `Report` (via a lazily-created system reporter
   `AppUser`, `B2CObjectId = "system:spam-detection"`, so it never collides with a real user) plus
   one `SpamFlag` row per triggered reason, routed into the same moderator report queue as
   user-submitted reports. **The post is still published** — spam detection flags for review, it
   doesn't block, which is the key behavioral difference from PII detection.

`CommentService.CreateAsync` runs the identical ban/restriction/PII/spam sequence (minus flair and
image handling, plus a `PostLockedException` check and parent-comment resolution for replies).

## 3. Image upload (direct-to-Blob via SAS)

The backend never receives raw image bytes — Kestrel's 1 MB request body cap would make that
impractical anyway (see [cross-cutting-concerns.md](cross-cutting-concerns.md)).

1. Client calls `POST /api/uploads/images/sas` (rate-limited: `ImageUpload`).
   `ImageUploadService.CreateUploadSasAsync` mints a blob path `{userId}/{new Guid}` and a
   short-lived (10-minute) SAS token scoped to `Write`+`Create` on exactly that blob, returning
   both the SAS upload URL and the plain blob URL.
2. Client uploads the image bytes directly to Azure Blob Storage using the SAS URL — the backend
   is not in this request path at all.
3. Client includes the resulting blob URL in `CreatePostRequest.ImageUrls`. `PostService.CreateAsync`
   validates it server-side (step 5 above) before persisting — ownership-prefix check, size cap,
   magic-byte signature check — since a write-only SAS can't itself enforce content-type or size
   limits at upload time.

## 4. Voting & ranking

- `PUT /api/posts/{id}/vote` / `PUT /api/comments/{id}/vote` (rate-limited: `Vote`) call
  `IVotingService.CastVoteAsync(VoteTargetType.Post|.Comment, targetId, voterId, value)` — one
  `Vote` row per `(TargetType, TargetId, VoterId)`, upserted.
- Listing endpoints (`GET /api/communities/{name}/posts?sort=...`) parse the `sort` query param via
  `SortOrderParser.Parse` — lenient by design, falling back to `SortOrder.New` on anything
  unrecognized rather than 400ing — then `RankingService.ApplyPostSort` resolves the matching
  `ISortStrategy` via keyed DI (`GetRequiredKeyedService<ISortStrategy>(sortOrder)`) and applies it
  as an `IQueryable<Post>` transform before pagination. Each strategy
  (`Services/Ranking/Strategies/*`) encapsulates one ordering algorithm (Hot, New, Top,
  Controversial, Trending, Pinned) independently.

## 5. Moderation report lifecycle

1. Any authenticated user calls `POST /api/posts/{id}/reports` or `/api/comments/{id}/reports`
   (rate-limited: `Report`) with a `ReportReasonCategory` and optional free-text details.
   `ReportService.CreateAsync` writes a `Report` with `Status = Open`.
2. A community moderator calls `GET /api/communities/{name}/mod/reports` (resource-authorized via
   `CommunityModerator`) — `ReportService.ListForCommunityAsync` resolves which reports belong to
   this community by first collecting the community's post/comment ids, since `Report` itself has
   no direct `CommunityId` column (it's reached via the reported post or the reported comment's
   parent post).
3. The moderator calls `POST /api/communities/{name}/mod/reports/{id}/resolve` with an outcome
   (`ActionTaken`/`Dismissed`). `ReportService.ResolveAsync` sets `Report.Status` accordingly and,
   only if `ActionTaken`, calls `IModerationActionService.RecordReportResolutionAsync` to write a
   `ModerationAction` audit row (`ActionType = ReportResolve`).
4. Separately, a moderator can act directly on the reported content (`POST /api/posts/{id}/mod/remove`,
   `/pin`, `/lock`, or the comment equivalent) — each action both mutates the entity
   (`Post.IsRemoved`/`IsPinned`/`IsLocked`, `Comment.IsRemoved`) and records a `ModerationAction`
   row via `ModerationActionService`. Removal additionally dispatches a `ModAction` notification to
   the content's author ("Your post was removed by a moderator.").
5. Platform-level equivalents (`PlatformRestriction`, via `/api/admin/users/{username}/restrict`)
   follow the same "mutate + audit row + notify" shape but aren't community-scoped
   (`ModerationAction.CommunityId = null`) and require the `PlatformAdmin` policy instead of
   `CommunityModerator`.

## 6. Friend requests

1. `POST /api/users/{username}/friend-requests` → `FriendshipService.RequestAsync`. Self-requests
   throw `SelfFriendRequestException`; an existing non-`Declined` friendship (pending or already
   accepted, in either direction) throws `FriendshipAlreadyExistsException`. A previously
   `Declined` friendship is deleted and replaced, letting a declined request be re-sent.
2. A new `Friendship` row is created with `Status = Pending`, and a `FriendRequest` notification is
   dispatched to the addressee (same dispatch-and-push mechanism as flow 7 below).
3. `POST /api/users/{username}/friend-requests/accept` / `/decline` flip `Status` accordingly;
   `DELETE /api/friends/{username}` removes an existing accepted friendship.

## 7. Real-time notification delivery

`NotificationDispatcher.DispatchAsync` is the single choke point every notification-producing flow
calls through (moderation actions, friend requests, replies/mentions via `CommentService`): it
writes a `Notification` row, then immediately pushes it over SignalR to
`NotificationHub.GroupName(recipientId)` — a per-user group every connected client for that user
joins on hub connect. This means notification delivery is "persist, then push" in the same call —
a client that's offline still gets the row via `GET /api/notifications` on next load; a client
that's connected gets it pushed live without polling. The hub connection itself authenticates via
the SignalR token-relay mechanism described in
[authentication-and-authorization.md](authentication-and-authorization.md).

## Related docs
- [architecture.md](architecture.md) — the services and DI patterns these flows are built from.
- [domain-model.md](domain-model.md) — the entities each flow reads/writes.
- [api-surface.md](api-surface.md) — the exact routes referenced above.
- [cross-cutting-concerns.md](cross-cutting-concerns.md) — rate limits, validation, and error shapes these flows produce on failure.
