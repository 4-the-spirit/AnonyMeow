# Backend documentation

Prose reference for the ASP.NET Core Minimal API backend (`backend/`) — how it's structured, how
requests flow through it, and the conventions it follows. These are narrative companions to the
visual diagrams in [`../diagrams/`](../diagrams/README.md), not a replacement for them: read a
diagram for the shape of something, read the matching doc here for *why* it's built that way.

## Start here

New to the backend? Read in this order:

1. **[architecture.md](architecture.md)** — the layer structure (`Domain`/`Data`/`Dtos`/`Endpoints`/`Services`),
   `Program.cs` as the composition root, and the design patterns actually in use. *(complements [diagram 03](../diagrams/03-backend-layered-architecture-and-pipeline.md))*
2. **[domain-model.md](domain-model.md)** — the 25 EF Core entities, how they relate, and the
   target-type-discriminator pattern shared by votes/reactions/reports/spam flags.
   *(complements [diagrams 04–05](../diagrams/README.md))*
3. **[authentication-and-authorization.md](authentication-and-authorization.md)** — CIAM/JWT setup,
   the dev-mode bypass, and the three custom authorization handlers.
   *(complements [diagrams 06–09](../diagrams/06-authorization-and-authentication-pipeline.md))*
4. **[api-surface.md](api-surface.md)** — every route, grouped by feature area, with auth/rate-limit notes.
5. **[flows.md](flows.md)** — step-by-step walkthroughs of the flows that cut across services:
   onboarding, post creation with PII/spam checks, image upload, voting/ranking, moderation report
   resolution, friend requests, real-time notification delivery.
6. **[cross-cutting-concerns.md](cross-cutting-concerns.md)** — exception handling, validation,
   logging, rate limiting, and a few gotchas worth knowing before you touch any of them.
7. **[testing.md](testing.md)** — unit vs. integration test conventions, and how to run both.

## Scope

This folder is backend-specific and prose-first. For the system-wide picture (Azure topology),
see [`docs/README.md`](../README.md) and
[`docs/diagrams/02-system-architecture.md`](../diagrams/02-system-architecture.md).
There is no dedicated CI/CD or frontend-architecture diagram in the current diagram set — see
`.github/workflows/*.yml` and `frontend/src/` directly for those.
For deployment steps, see [`docs/deployment-guide.md`](../deployment-guide.md) and
[`docs/azure-ciam-setup.md`](../azure-ciam-setup.md).

Like the diagrams, these docs are hand-written from the source as of the date they were added —
not auto-generated, and not guaranteed to stay in sync automatically. When you make a structural
change (a new endpoint group, a new entity, a changed auth policy), update the relevant doc in the
same change rather than deferring it.
