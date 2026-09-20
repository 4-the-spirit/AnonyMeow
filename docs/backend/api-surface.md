# API surface

Every route registered in `backend/Endpoints/*.cs`, grouped by feature area in the order
`Program.cs` mounts them. Auth column: **Anon** = `AllowAnonymous()`, **Auth** = default fallback
policy (authenticated + completed profile), a named policy is spelled out where used. Rate limit
column names the `RequireRateLimiting("...")` policy where one applies — see
[cross-cutting-concerns.md](cross-cutting-concerns.md) for what the limits actually are.

## Health

| Route | Method | Auth |
|---|---|---|
| `/health` | GET | Anon |

## Users (`UserEndpoints.cs`)

| Route | Method | Auth | Notes |
|---|---|---|---|
| `/api/auth/complete-profile` | POST | `AuthenticatedOnly` | Onboarding — see [flows.md](flows.md) |
| `/api/users/me` | GET / PATCH | Auth | |
| `/api/users/check-username` | GET | `AuthenticatedOnly` | Availability check |
| `/api/users/{username}` | GET | Anon | |
| `/api/users/{username}/posts` | GET | Anon | |
| `/api/users/{username}/comments` | GET | Anon | |
| `/api/users/{username}/communities` | GET | Anon | |

## Communities (`CommunityEndpoints.cs`, group `/api/communities`)

| Route | Method | Auth |
|---|---|---|
| `/` | POST | Auth |
| `/` | GET (search) | Anon |
| `/{name}` | GET | Anon |
| `/{name}` | PATCH | Auth + `CommunityModerator` (resource-based) |
| `/{name}/join` | POST | Auth |
| `/{name}/leave` | DELETE | Auth |
| `/{name}/moderators` | GET | Anon |
| `/{name}/members` | GET | Anon |

`CreateCommunityAsync` also does hand-rolled validation ahead of the service call: name pattern
(Hebrew letters/digits/spaces/underscores/hyphens, 3–30 chars), description length, per-rule
title/description bounds, per-flair name/color + no-duplicate/no-default-name-collision, and
required icon/banner images (waived only in Development, where blob storage isn't provisioned).

## Flairs (`FlairEndpoints.cs`, group `/api/communities/{name}/flairs`)

| Route | Method | Auth |
|---|---|---|
| `/` | POST | Auth |
| `/` | GET | Anon |
| `/{id}` | PATCH | Auth |
| `/{id}` | DELETE | Auth |

## Posts (`PostEndpoints.cs`)

| Route | Method | Auth | Rate limit |
|---|---|---|---|
| `/api/communities/{name}/posts` | POST | Auth | `CreatePost` |
| `/api/communities/{name}/posts` | GET | Anon | |
| `/api/posts/{id}` | GET | Anon | |
| `/api/posts/{id}` | PATCH | Auth (author only) | |
| `/api/posts/{id}` | DELETE | Auth (author only) | |
| `/api/posts/{id}/poll-votes` | POST | Auth | |
| `/api/posts/{id}/vote` | PUT / DELETE | Auth | `Vote` (PUT only) |
| `/api/uploads/images/sas` | POST | Auth | `ImageUpload` |
| `/api/posts/{id}/flair` | PATCH | Auth (author only) | |
| `/api/posts/{id}/reactions/{emoji}` | PUT / DELETE | Auth | |

Listing/detail endpoints assemble a `PostResponse` by fanning out to `IVotingService` (score,
viewer's vote), `ICommentService` (comment count), `IFlairService`, `IReactionService`, plus
direct `AppDbContext` lookups for author/community/images — there's no single "get full post"
query; it's composed per-request in the endpoint handler.

## Comments (`CommentEndpoints.cs`)

| Route | Method | Auth | Rate limit |
|---|---|---|---|
| `/api/posts/{postId}/comments` | POST | Auth | `CreateComment` |
| `/api/posts/{postId}/comments` | GET (top-level) | Anon | |
| `/api/comments/{id}` | GET | Anon | |
| `/api/comments/{id}/replies` | GET | Anon | |
| `/api/comments/{id}` | PATCH / DELETE | Auth (author only) | |
| `/api/comments/{id}/vote` | PUT / DELETE | Auth | `Vote` (PUT only) |
| `/api/comments/{id}/reactions/{emoji}` | PUT / DELETE | Auth | |

## Moderation (`ModerationEndpoints.cs`)

| Route | Method | Auth | Rate limit |
|---|---|---|---|
| `/api/posts/{id}/reports` | POST | Auth | `Report` |
| `/api/comments/{id}/reports` | POST | Auth | `Report` |
| `/api/communities/{name}/mod/reports` | GET | `CommunityModerator` | |
| `/api/communities/{name}/mod/reports/{id}/resolve` | POST | `CommunityModerator` | |
| `/api/posts/{id}/mod/pin` \| `/lock` | POST / DELETE | `CommunityModerator` | |
| `/api/posts/{id}/mod/remove` | POST | `CommunityModerator` | |
| `/api/comments/{id}/mod/remove` | POST | `CommunityModerator` | |
| `/api/communities/{name}/mod/bans` | POST / GET | `CommunityModerator` | |
| `/api/communities/{name}/mod/bans/{username}` | DELETE | `CommunityModerator` | |
| `/api/communities/{name}/mod/moderators/{username}` | POST / DELETE | `CommunityModerator` | |

`CommunityModerator` here means the endpoint resolves the target `Community` first, then calls
`IAuthorizationService.AuthorizeAsync(user, community, "CommunityModerator")` inline and returns
`Forbid()` on failure — not a route-level attribute, since the community has to be looked up (by
name, or via the post/comment's community) before the resource-based check can run. See the
moderation report lifecycle in [flows.md](flows.md).

## Admin (`AdminEndpoints.cs`, group `/api/admin`, `.RequireAuthorization("PlatformAdmin")` on the whole group)

| Route | Method |
|---|---|
| `/reports` | GET |
| `/spam-flags` | GET |
| `/users/{username}/restrict` | POST / DELETE |
| `/audit-log` | GET |

## Friends & Blocks (`FriendshipEndpoints.cs`, `BlockEndpoints.cs`)

| Route | Method | Auth |
|---|---|---|
| `/api/users/{username}/friend-requests` | POST / DELETE | Auth |
| `/api/users/{username}/friend-requests/accept` \| `/decline` | POST | Auth |
| `/api/friends/{username}` | DELETE | Auth |
| `/api/users/{username}/friends` | GET | Auth |
| `/api/users/me/friend-requests` | GET | Auth |
| `/api/users/{username}/block` | POST / DELETE | Auth |

## Conversations & Messages (`ConversationEndpoints.cs`, group `/api/conversations`; `MessageEndpoints.cs`)

| Route | Method | Auth | Rate limit |
|---|---|---|---|
| `/api/conversations` | POST / GET | Auth | `SendMessage` (POST) is on the messages route, not here |
| `/api/conversations/{id}` | GET / DELETE | Auth | |
| `/api/conversations/{id}/messages` | GET / POST | Auth | `SendMessage` (POST) |
| `/api/conversations/{id}/pin` | PATCH | Auth | |
| `/api/messages/{id}/reports` | POST | Auth | `Report` |

## Notifications (`NotificationEndpoints.cs`, group `/api/notifications`)

| Route | Method | Auth |
|---|---|---|
| `` (list) | GET | Auth |
| `/{id}/read` | POST | Auth |
| `/read-all` | POST | Auth |

Real-time delivery is via SignalR (`/hubs/notifications`, `NotificationHub`), not polling — see
[flows.md](flows.md).

## Saved posts/comments & Feed (`SavedPostEndpoints.cs`, `SavedCommentEndpoints.cs`, `FeedEndpoints.cs`)

| Route | Method | Auth |
|---|---|---|
| `/api/posts/{id}/save` | POST / DELETE | Auth |
| `/api/users/me/saved-posts` | GET | Auth |
| `/api/comments/{id}/save` | POST / DELETE | Auth |
| `/api/users/me/saved-comments` | GET | Auth |
| `/api/feed` | GET | Anon |

## Discover & Search (`DiscoverEndpoints.cs`, `SearchEndpoints.cs`)

| Route | Method | Auth |
|---|---|---|
| `/api/discover/trending` | GET | Anon |
| `/api/discover/recommended-communities` | GET | Anon |
| `/api/search` | GET | Anon |

## Development-only (`DevAuthEndpoints.cs`)

| Route | Method | Auth |
|---|---|---|
| `/api/dev/token` | POST | Anon, but only mapped when `app.Environment.IsDevelopment()` |

Issues a self-signed JWT from `DevJwtTokenFactory` — see
[authentication-and-authorization.md](authentication-and-authorization.md).

## Real-time (`Hubs/NotificationHub.cs`)

| Route | Protocol |
|---|---|
| `/hubs/notifications` | SignalR (WebSocket/SSE, token via `?access_token=` query param) |

## Related docs
- [flows.md](flows.md) — end-to-end request/response flows through several of these groups.
- [authentication-and-authorization.md](authentication-and-authorization.md) — what `Auth`, `AuthenticatedOnly`, `CommunityModerator`, `PlatformAdmin` actually check.
- [cross-cutting-concerns.md](cross-cutting-concerns.md) — rate limit values, validation status codes, error shapes.
