# Backend Diagram Set

Sixteen diagrams (20 images total — a few topics split into two) covering the backend's
architecture, domain model, auth, and key request flows. Each `.md` file below pairs a rendered
image with the narrative explaining *why* it's built that way; the images share one consistent
theme (`source/mermaid-theme.json`) so the set reads as a single document rather than a pile of
disconnected sketches.

Source: hand-written [Mermaid](https://mermaid.js.org/) diagrams in [`source/`](source/), rendered
to PNG via `@mermaid-js/mermaid-cli`. They're narrative companions to the prose docs in
[`../backend/`](../backend/) — read a diagram for the shape of something, read the matching doc for
the reasoning.

## Index

| # | Diagram | Covers |
|---|---------|--------|
| 02 | [System / Container Architecture](02-system-architecture.md) | System/container view — SPA, API, SignalR, Postgres, Blob Storage, CIAM |
| 03 | [Layered Architecture & Pipeline](03-backend-layered-architecture-and-pipeline.md) | Middleware pipeline order + Endpoints → Services → Data layering |
| 04 | [Domain Model — Core Content](04-domain-model-core-content.md) | ER diagram: users, communities, posts, comments, votes, reactions, polls, saves |
| 05 | [Domain Model — Social & Moderation](05-domain-model-social-moderation.md) | ER diagram: friendships, blocks, DMs, notifications, reports, moderation, PII log |
| 06 | [Authorization & Authentication Pipeline](06-authorization-and-authentication-pipeline.md) | JWT validation → JIT user provisioning → policy evaluation |
| 07 | [Auth: Sign-Up Flow](07-auth-signup-flow.md) | Native auth sign-up: start → email OTP → token exchange |
| 08 | [Auth: Sign-In Flow](08-auth-signin-flow.md) | Native auth sign-in: initiate → password challenge → tokens |
| 09 | [Auth: Password Reset & Refresh](09-auth-password-reset-and-refresh-flow.md) | Password reset (OTP + async poll) and refresh-token exchange |
| 10 | [Post Creation Flow](10-post-creation-flow.md) | Image SAS upload → create post → PII gate → persist → spam flag |
| 11 | [Comment & Notification Flow](11-comment-and-notification-flow.md) | Create comment → mentions → notification dispatch → SignalR push |
| 12 | [Content Safety Pipeline](12-content-safety-pipeline.md) | PII detection (pre-persist block) + spam detection (post-persist flag), Composite pattern |
| 13 | [Voting & Reactions Flow](13-voting-and-reactions-flow.md) | Vote casting/removal with karma, and reactions |
| 14 | [Feed & Ranking Strategy](14-feed-and-ranking-strategy.md) | Feed/sort Strategy pattern via keyed DI (Hot/Top/New/Controversial/Trending/Pinned) |
| 15 | [Messaging Flow](15-messaging-flow.md) | Start conversation → block re-check → send message → SignalR delivery |
| 16 | [Friendship & Blocking Flow](16-friendship-and-blocking-flow.md) | Friendship status lifecycle (state diagram) + block flow |
| 17 | [Moderation & Admin Flow](17-moderation-and-admin-flow.md) | Report → resolve → moderation action; community ban vs. platform restriction |

## Regenerating

Diagrams are hand-written from the source, not auto-exported — if one looks wrong after a
refactor, treat it as documentation to update. To re-render after editing a `.mmd` file:

```bash
npx --yes @mermaid-js/mermaid-cli \
  -i docs/diagrams/source/<name>.mmd \
  -o docs/diagrams/images/<name>.png \
  -c docs/diagrams/source/mermaid-theme.json \
  -b white -s 3
```

(On Windows, mermaid-cli's bundled Chromium may fail to launch — point
`PUPPETEER_EXECUTABLE_PATH` at a system Chrome install if so.)
