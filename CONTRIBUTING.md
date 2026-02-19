# Contributing to EchoHub

Thanks for your interest in contributing — EchoHub aims to stay lightweight, terminal-first, and self-hosted.

## Quick start (dev)

### Prerequisites

- .NET 10 SDK

### Build

```bash
dotnet build src/EchoHub.slnx
```

### Test

```bash
dotnet test src/EchoHub.slnx
```

### Run (local)

Server:

```bash
dotnet run --project src/EchoHub.Server
```

Client:

```bash
dotnet run --project src/EchoHub.Client
```

## What to work on

- Check open issues (especially `good first issue` / `help wanted` if present)
- Docs fixes in `docs/` are always welcome
- Tests: `src/EchoHub.Tests/`

If you’re proposing a larger change, open an issue first so we can align on approach.

## Code style & expectations

- Keep PRs focused (small and reviewable)
- Prefer clear naming over cleverness
- Add/adjust tests for bug fixes when it’s practical
- Avoid committing secrets (JWT secrets, tokens, connection strings)

## Docs

This repo uses DocFX for the site in `docs/`.

To build docs locally you typically need the assemblies built in Release first:

```bash
dotnet build src/EchoHub.slnx --configuration Release
```

Then run DocFX:

```bash
docfx docs/docfx.json
```

## Markdown lint

CI lints Markdown. Locally:

- On Linux/macOS (or Windows with Git Bash/WSL):

```bash
./scripts/lint-markdown.sh
```

- Anywhere with Node.js installed:

```bash
npx --yes markdownlint-cli2
```

## Pull requests

- Fill out the PR template
- Ensure `dotnet test src/EchoHub.slnx` is green
- Mention any behavioral changes (client UX, auth, uploads)

## Commit messages

Any consistent style is fine; descriptive subjects help reviews.
Examples:

- `fix(server): validate image magic bytes`
- `feat(client): add /servers improvements`
- `docs: clarify getting started`

## Reporting security issues

Please do **not** file public issues for security problems. See `SECURITY.md`.
