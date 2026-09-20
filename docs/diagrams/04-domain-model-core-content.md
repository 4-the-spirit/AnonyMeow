# Domain Model — Core Content & Community (ER Diagram, part 1 of 2)

Entities for identity, communities, posts, comments, voting, reactions, polls, and saves.
Social/messaging/moderation entities are in
[05-domain-model-social-moderation.md](05-domain-model-social-moderation.md) — `AppUser` and
`Community` are the shared anchors between the two diagrams.

![Domain Model — Core Content & Community (ER Diagram, part 1 of 2)](images/04-domain-model-core-content-1.png)

## Notes

- **Composite primary keys**: `CommunityMembership`, `CommunityBan`, `SavedPost`, `SavedComment`
  have no surrogate `Id` — the pair of FK columns *is* the key (shown as two `PK` fields).
- **Polymorphic targets**: `Vote.TargetType`/`TargetId` and `Reaction.TargetType`/`TargetId` point
  at either a `Post` or a `Comment` depending on `TargetType` — there's no DB-level FK constraint
  enforcing this, it's application-level (`VoteTargetType`/`ReactionTargetType` enums).
  `Vote` and `Reaction` are both unique per `(TargetType, TargetId, VoterId/AppUserId)` — one vote
  and one active reaction-emoji per user per target.
  `PollVote` is unique per `(PostId, AppUserId)` — single-select polls.
- Full source: `backend/Domain/*.cs`, `backend/Domain/Enums/*.cs`, EF configuration in
  `backend/Data/Configurations/*.cs`.
