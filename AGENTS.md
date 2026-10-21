# mix-server

ASP.NET Core 10 backend with an Angular 22 client. The production image includes
ffmpeg and serves the Angular production build.

## Always
- Run `make test-unit` for the containerized backend and frontend build check.
  It uses baked-in source, so no host .NET or Node installation is required.
- Run `make test-image` after Dockerfile changes.
- Keep EF Core migrations and the model snapshot in sync.
- Regenerate generated TypeScript clients with
  `pwsh scripts/regenerate_api_clients.ps1` after API-controller or DTO changes.
- Follow existing C# conventions: four-space indentation, `_camelCase` private
  fields, and `Async` method suffixes.

## Never
- Mount, read, or write local `data/`, media, SQLite databases, or
  `src/api/MixServer/appsettings.Local.json` in test commands or containers.
- Change dependency versions, GitHub Actions, or generated clients unless the
  requested work requires it.
- Claim unit tests ran when no test project exists. `make test-unit` currently
  builds the backend and Angular production client and reports that fact.

## Commands
- `make test-unit` — isolated containerized backend and frontend build check.
- `make test-image` — production Docker image build.
- `make test-all` — both checks.
