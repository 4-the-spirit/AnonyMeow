# AnonyMeow — Development Rules

## Project overview
AnonyMeow is a full-stack monolith application:
- **Backend:** ASP.NET Core Minimal API (C#)
- **Frontend:** React.js
- **Architecture:** Monolith
- **Database:** PostgreSQL
- **Cloud provider:** Microsoft Azure
- **Authentication:** OpenID Connect

This file is the source of truth for how to work in this repo. Update it as conventions evolve.

## Verification rules
- After any backend change, run `dotnet build` and fix all warnings/errors before moving on.
- After any backend change that touches an endpoint, exercise the affected endpoint(s) end-to-end via the Postman MCP server before considering the change done — unit tests alone are not sufficient.
- After any backend change, run the xUnit test suite (`dotnet test`), covering both:
  - **Unit tests** — business logic in isolation.
  - **Integration tests** — via `WebApplicationFactory`, hitting a real or containerized PostgreSQL instance.
  All tests must pass.
- Backend CI runs on `ubuntu-latest`, but local dev is often Windows — OS-dependent bugs (e.g. `Uri` parsing, path/case-sensitivity) can pass locally and fail only in GitHub Actions. Before pushing changes at risk of this (anything touching URL/path/string parsing or culture-sensitive logic), run `scripts/test-like-ci.ps1`, which runs the real build + test suite inside a Linux container matching the CI runner.
- New backend functionality must ship with corresponding unit and/or integration tests in the same change — don't defer test-writing to "later."
- After any frontend change, run the frontend test suite and linter, and manually exercise the affected UI flow in a browser before reporting the task complete.
- Never mark a task complete if build, tests, or Postman verification were skipped — state explicitly what was and wasn't verified.

## Backend conventions (ASP.NET Core Minimal API)
- Group related endpoints with `MapGroup` / extension methods instead of one giant `Program.cs`.
- Use DTOs/records for request and response shapes — never expose EF Core entities directly over the wire.
- Validate all external input at the API boundary (model validation or FluentValidation); return `ProblemDetails` for errors.
- Keep endpoint handlers thin; put business logic in services injected via DI.

## Design principles and patterns
- Apply SOLID at all times:
  - **S**ingle Responsibility — one reason to change per class/module; split services when they accumulate unrelated responsibilities.
  - **O**pen/Closed — extend behavior via new classes/strategies rather than editing existing tested logic.
  - **L**iskov Substitution — derived types/implementations must be usable wherever the base/interface is expected, without surprising behavior.
  - **I**nterface Segregation — prefer small, focused interfaces over large ones clients must partially implement.
  - **D**ependency Inversion — depend on abstractions (interfaces) via constructor injection, not concrete implementations; register them in DI.
- Favor composition over inheritance.
- Reach for an established design pattern (Repository, Strategy, Factory, Decorator, Mediator/CQRS, Options pattern for config, etc.) when it genuinely fits the problem — don't force a pattern where a plain function/class is clearer, and don't introduce abstractions speculatively for hypothetical future needs.
- Keep cross-cutting concerns (logging, validation, error handling, auth) in middleware/pipeline behaviors rather than duplicated per endpoint.
- Prefer clear, descriptive naming and small, single-purpose functions/classes over clever or overly generic ones.

## Database (PostgreSQL) conventions
- All schema changes go through EF Core migrations, generated and committed — never hand-edit the DB schema.
- Migrations must be reviewed for destructive operations (dropped columns/tables) before applying.
- Local dev and integration tests use a real PostgreSQL instance (e.g., via Docker) — not an in-memory/fake provider — so behavior matches production.
- Connection strings and credentials come from configuration/secrets, never hardcoded.

## Authentication (OpenID Connect)
- All protected endpoints must require and validate JWT/OIDC tokens via ASP.NET Core's built-in authentication middleware — no custom token parsing.
- Never log tokens, secrets, or PII.
- Authorization checks (roles/policies) belong in policy definitions, not scattered inline checks.

## Frontend (React) conventions
- Co-locate components, tests, and styles; keep API calls in a dedicated service/hooks layer, not inline in components.
- Handle loading/error states for every API call.

## Security baseline
- No secrets, connection strings, or API keys committed to source; use environment variables / Azure Key Vault / user-secrets locally.
- HTTPS-only; CORS restricted to known frontend origins.
- Sanitize/validate all user input to guard against injection (SQL injection via EF parameterization, XSS on the frontend).

## Cloud / Azure
- Configuration differences between environments (dev/staging/prod) go through Azure App Configuration or environment variables — not code branches.
- Treat infrastructure changes (App Service settings, DB firewall rules, Key Vault access policies) as requiring explicit user confirmation before applying, since they affect shared/live resources.

## Git / workflow
- Small, focused commits; one logical change per commit.
- Every change that touches the backend must pass build + xUnit tests + Postman verification before being committed.

## Execution Log
- Maintain a running record of implemented work in an `Execution Log/` folder at the repo root.
- Whenever a feature or notable change is implemented, add one new markdown file to `Execution Log/` named `YYYY-MM-DD-short-slug.md` (don't append to prior entries — one file per change).
- Each entry should summarize: what was implemented, the key files/areas touched, and what was and wasn't verified (build/tests/Postman), matching the verification rules above.
- Keep entries concise — a few bullet points, not a full narrative. This log is for tracking what shipped over time, not a substitute for commit messages or the execution plan docs.
