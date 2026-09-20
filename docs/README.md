# AnonyMeow

AnonyMeow is an anonymous, Reddit-style forum. Users sign up with an email (used only for
authentication/recovery — never shown publicly) and are represented everywhere else by a
permanent, unique username, a changeable display name, and a DiceBear avatar. Content is
organized into topic communities with threaded comments, voting, reactions, a pseudonym-based
friend graph, private messaging, and moderation tooling — see [FEATURES.md](FEATURES.md) for
the full feature spec.

## Architecture

A full-stack monolith:

- **Backend:** ASP.NET Core Minimal API (C#, .NET 10)
- **Frontend:** React 19 + TypeScript + Vite
- **Database:** PostgreSQL (via EF Core / Npgsql)
- **Real-time:** SignalR (notifications, messaging)
- **Auth:** OpenID Connect / Microsoft Entra External ID (CIAM, JWT bearer)
- **Cloud provider:** Microsoft Azure

See [CLAUDE.md](../CLAUDE.md) for detailed development conventions.

For a deeper written walkthrough of the backend specifically — architecture, domain model, auth,
API surface, key flows, cross-cutting concerns, and testing — see
[backend/README.md](backend/README.md). Visual diagrams (system architecture, request
pipeline, auth sequences, ERDs, content/social flows, moderation) live in
[diagrams/](diagrams/README.md).

## Project layout

```
backend/     ASP.NET Core Minimal API solution (API project + xUnit unit/integration tests)
frontend/    React + TypeScript + Vite single-page app
infra/       Bicep templates for Azure resource provisioning
docs/        This documentation, diagram sources/renders, and portfolio media
```

## Getting started

### Backend

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download), PostgreSQL (or Docker, for
integration tests which use Testcontainers).

```bash
cd backend
dotnet build "AnonyMeow.sln"
dotnet test "AnonyMeow.sln"
dotnet run --project "AnonyMeow.csproj" --launch-profile https
```

Configure secrets (connection strings, CIAM settings, blob storage) via `dotnet user-secrets`
locally — see `appsettings.json` for the expected configuration shape. Never commit real secrets.

### Frontend

Requirements: Node.js.

```bash
cd frontend
npm install
npm run dev       # start the Vite dev server
npm run test      # run unit tests (Vitest)
npm run lint      # lint (Oxlint)
```

See [frontend/README.md](../frontend/README.md) for template-specific notes.

## Contributing

Development rules (build/test/verification requirements, coding conventions, SOLID/design
guidance, DB migration policy, etc.) are documented in [CLAUDE.md](../CLAUDE.md) — read it
before making changes.
