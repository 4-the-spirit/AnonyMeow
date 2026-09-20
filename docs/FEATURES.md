# AnonyMeow — Feature List

## Context

AnonyMeow is an anonymous, Reddit-like forum. This document is a feature brainstorm/spec —
no architecture, data model, or implementation decisions are locked in yet. Those come once
the feature set below is agreed on.

Decisions locked in so far:
- **Anonymity model: persistent pseudonym, backed by an email account.** Users sign up with
  an email (used only for authentication/account recovery — never shown to anyone). Their
  public identity is a permanent, unique **username**, a changeable **display name**, and an
  avatar. Real name, email, and phone number are never displayed publicly.
- **Content shape: Reddit-style communities.** Topic-based communities ("subs"), mixed post
  types, threaded comments.
- **Pseudonym-based social graph.** Users can add each other as friends; a friend list can be
  shown on a profile, gated by a privacy setting the profile owner controls.
- **Moderation stance: transparent, not silent.** Enforcement should notify the user and
  suspend/restrict them, rather than shadow-banning without their knowledge.
- The name/"Meow" branding suggests a lighter, more casual tone than Reddit; this list keeps
  that in mind but doesn't lock in visual branding.

## System Characterization

AnonyMeow's anonymity guarantee is about what's shown publicly, not about avoiding accounts
altogether. Users sign up with an email like on most platforms, but that email is purely for
authentication and recovery — it's never displayed, and no real name or phone number field
exists anywhere in the public product surface. What others see is always a permanent, unique
username, a changeable display name, and an avatar. This is close to how Reddit already works
in practice; AnonyMeow's differentiator is enforcing that pseudonym boundary structurally
(no real-name field to ever leak, PII detection on submitted content, a deliberately permanent
username so identity can't be casually cycled) rather than leaving it to user discipline.

That shapes the rest of the system in a few concrete ways:

- **Accountability through permanence, not disposability.** Because usernames are permanent
  and unique per account, moderation actions actually stick — there's no lightweight "get a
  new pseudonym" action. Starting over requires creating an entirely new account with a new
  email, which is real (if not absolute) friction against ban evasion. This is also why the
  moderation stance favors transparent warnings/suspensions over shadow-banning: identity now
  carries enough weight that direct enforcement is meaningful.
- **Communities as the primary organizing unit**, alongside a pseudonym-to-pseudonym friend
  graph. Like Reddit, the main structure users navigate is topic communities; the friend graph
  is a secondary, opt-in-visibility layer on top, never tied to real identity.
- **Safety is structural, not just reactive.** PII detection at submission time, block/report
  reaching into private messages, and moderation tooling that notifies users of enforcement
  rather than hiding it, all exist to close gaps proactively rather than only after harm is
  done.
- **Tone.** The "AnonyMeow" name signals something lighter and more playful than Reddit's
  utilitarian feel. This shows up mainly in optional/cosmetic surface area (the avatar system,
  branding) rather than in the functional feature set, so it doesn't box in future design
  decisions.

## Feature List

### 1. Identity & Anonymity

- **Email-based signup, pseudonymous public identity.** Users create an account with an email
  (authentication/recovery only — never shown publicly). On top of that account, they choose a
  **username** (permanent, globally unique, cannot be changed later) and a **display name**
  (freely changeable, doesn't need to be unique). Posts, comments, votes, and reactions are
  always attributed to the username/display name, never the email or any real-world identifier.
- **Avatar via DiceBear.** Avatars are generated using [DiceBear](https://github.com/dicebear/dicebear),
  an open-source avatar library — users pick a style/seed (and regenerate/customize within
  what DiceBear supports) rather than a from-scratch piece-by-piece builder. *(Open idea: pick
  a DiceBear style that leans into the "Meow" branding — a taste call, not decided here.)*
- **No lightweight identity reset.** Because usernames are permanent and unique, there's no
  in-app "get a new pseudonym" action. Starting over means creating a new account with a new
  email — an intentional bar against casually shedding bans or bad reputation.
- **Pseudonym-based friends.** Users can add each other as friends (pseudonym-to-pseudonym,
  never tied to real identity). A user's friend list can be shown on their profile, controlled
  by a privacy setting the profile owner sets (see Profile settings, Section 5) — visible to
  everyone, no one, or some narrower scope, at the owner's choice.

### 2. Communities & Content

- **Topic communities.** Any user can create a community (no special permissions or approval
  gate assumed at this stage), and any user can browse, join, or leave one. Each community has
  its own description and rules, similar to a subreddit, and acts as the primary way content is
  organized and discovered.
- **Multiple post types.** A post can be a text discussion, a question, a link, an image, or a
  poll — all behave as standard Reddit-style posts (no accepted-answer mechanic; see Section 7
  for that as an open idea rather than a committed feature).
- **Threaded, nested comments.** Comments can reply to the post or to other comments, forming
  a tree, with collapse/expand so long threads stay navigable.
- **Voting.** Posts and comments can be upvoted or downvoted, driving ranking (see Sorting)
  and, indirectly, reputation (see Section 3).
- **Emoji reactions.** Independent of voting, users can react to comments (and posts) with
  emoji — a lighter-weight, more expressive signal than a vote, useful for reactions that
  aren't really "this is good/bad content" (e.g., 😂, 😮, ❤️).
- **Basic formatting.** Posts and comments support lightweight formatting — bold, italic,
  strikethrough, links, block quotes, lists, and inline code — via either a minimal markdown
  syntax or a lightweight WYSIWYG toolbar. Scope is intentionally basic (not a full rich-text
  editor).
- **Sorting.** Feeds and comment threads can be sorted hot / new / top / controversial, matching
  the sorting vocabulary users already expect from Reddit-like platforms.
- **Flair/tags.** Posts can carry tags such as "Question" or "Discussion" to help with scanning
  and filtering within a community.
- **Search.** Users can search across posts, comments, and communities to find existing
  discussions rather than duplicating them.

### 3. Reputation & Trust

- **Karma/reputation score**, attached to the username rather than any real identity,
  aggregated from votes received on posts/comments.
- *(Stretch, not needed yet)* **Badges/achievements**, tied to the username, marking notable
  milestones or behaviors — deferred until there's a concrete need for them.

### 4. Moderation & Safety

This is the hardest problem given full anonymity, and deserves extra design attention beyond
this feature-list pass:

- **Community moderators.** Each community has moderators who can remove or pin posts, lock
  threads, and ban a username from that specific community.
- **Reporting.** Users can report posts, comments, or (see Section 5) direct messages for
  review by moderators/admins.
- **Rate limiting & spam detection.** Posting and voting are throttled to slow down brigading,
  bot activity, and vote manipulation.
- **Transparent enforcement.** Rather than shadow-banning, users who violate rules are
  notified about the offending activity and are suspended or otherwise restricted (temporarily
  or permanently) in a visible way — they know what happened and why.
- **Platform-level admin tools.** A global moderation queue and the ability to issue
  cross-community actions, for abuse that isn't confined to a single community. Cross-community
  action should be gentler than an in-community ban (e.g., a temporary platform-wide posting
  restriction rather than a full account suspension), reserving the harshest response for the
  community actually affected.
- **Ban-evasion friction.** The permanent-username-plus-email-account model (Section 1) already
  raises the cost of evasion compared to a freely resettable identity — a determined abuser
  still needs a new email/account to come back. Whether additional detection (e.g., duplicate-
  account signals) is worth adding is a later design question, not a baseline requirement.
- **PII detection on submission.** Post, comment, and direct-message content is scanned for
  patterns that look like private data — email addresses, phone numbers, and similar — before
  it's published or sent, and the submission is blocked with a warning rather than silently
  allowed through. This is somewhat ironic for an anonymous platform to need, but important:
  it protects users from accidentally deanonymizing themselves (or exposing someone else's
  private information) inside content that's otherwise meant to be anonymous.

### 5. Engagement

- **Notifications.** Replies and mentions are routed to the relevant username.
- **Saved/bookmarked posts**, for a user to revisit later.
- **Personal feed**, aggregated from the communities a user has joined.
- **Mentions** (@username) inside comments, to reference another user directly.
- **Private messaging.** Users can message each other directly. Because DMs are a common
  harassment vector on anonymous platforms, this is paired with a **block user** capability,
  and the reporting/moderation tooling from Section 4 explicitly extends to DMs rather than
  covering public content only. PII detection (Section 4) also applies to DM content.
- **Personal profile / activity area.** A user's profile shows the posts and comments they've
  created or participated in, with pagination, plus their friend list if they've chosen to make
  it visible. This is viewable both by the profile owner and by other users (scoped strictly to
  username/display-name identity — never to email or any real identifier).
- **Profile settings.** Users can edit their own profile — display name, avatar, notification
  settings, and privacy controls (e.g., friend list visibility) — from their personal area.
  Username is excluded from what's editable, since it's permanent.

### 6. Discovery

- **Trending/popular feed**, aggregated across all communities, for content surfacing outside
  a user's own joined communities.
- **Community recommendations**, based on activity, to help users find relevant communities.
- **Global search**, covering posts, comments, and communities (same capability referenced in
  Section 2, called out here as a discovery mechanism as well as a lookup one).

### 7. Open Differentiator Questions (vs. Reddit)

A few angles worth revisiting before finalizing scope:

- Time-boxed/ephemeral communities or posts that auto-archive?
- A lighter, more casual tone/branding (matching "AnonyMeow") vs. Reddit's utilitarian feel?
- Deeper Q&A mechanics (accepted answers, bounties) as a differentiator — previously
  considered and dropped from this pass, but worth revisiting later if Q&A becomes a bigger
  part of the vision.

## Next Steps

This is a features brainstorm only. A follow-up pass should map this list to concrete backend
entities and API surface on top of the existing ASP.NET Core scaffold.
