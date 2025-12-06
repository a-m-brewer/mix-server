# Contributing to Mix Server

Thank you for your interest in contributing to Mix Server! This guide will help you set up your development environment and understand the project structure.

## Quick Start

The fastest way to set up your development environment is to use the automated setup script:

```powershell
pwsh scripts/dev-setup.ps1
```

This script will:
- Check all prerequisites (.NET SDK, Node.js, npm, PowerShell Core)
- Create the `data/` directory for local development
- Create `appsettings.Local.json` with default configuration
- Install Angular dependencies
- Build the Angular client
- Link the wwwroot directory to the Angular build output
- Restore .NET dependencies

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js LTS (22+)** - [Download](https://nodejs.org/)
- **PowerShell Core** - [Download](https://github.com/PowerShell/PowerShell/releases)
- **ffmpeg** (optional, for transcoding features) - [Download](https://ffmpeg.org/download.html)

## Manual Setup

If you prefer to set up manually or need to troubleshoot, follow these steps:

### 1. Clone the Repository

```bash
git clone https://github.com/a-m-brewer/mix-server.git
cd mix-server
```

### 2. Create Data Directory

```bash
mkdir data
mkdir data/media
```

The `data/` directory is where:
- SQLite database will be stored
- Cache files will be kept
- You can place sample audio files in `data/media/` for testing

### 3. Configure Local Settings

Create `src/api/MixServer/appsettings.Local.json`:

```json
{
  "RootFolder": {
    "Children": "../../../data/media"
  }
}
```

You can also copy the template:
```bash
cp src/api/MixServer/appsettings.Local.json.template src/api/MixServer/appsettings.Local.json
```

**Note:** `appsettings.Local.json` is gitignored and will not be committed to the repository.

### 4. Install Angular Dependencies

```bash
cd src/clients/mix-server-client
npm ci
```

### 5. Build Angular Client

```bash
npx ng build
```

### 6. Link wwwroot

From the repository root:

```powershell
pwsh scripts/link_wwwroot.ps1
```

This creates a symbolic link from the .NET project's wwwroot directory to the Angular build output.

### 7. Restore .NET Dependencies

```bash
dotnet restore MixServer.sln
```

### 8. Build the Solution

```bash
dotnet build MixServer.sln
```

## Running the Application

### Development Mode (Recommended)

Run both the backend and frontend with live reload:

**Terminal 1 - Backend:**
```bash
dotnet watch --project src/api/MixServer/MixServer.csproj
```

**Terminal 2 - Frontend:**
```bash
cd src/clients/mix-server-client
npx ng serve
```

Access:
- **Angular UI**: http://localhost:4200
- **API**: http://localhost:5225
- **API Swagger**: http://localhost:5225/swagger

### Production Mode

Build and run the combined application:

```bash
# Build Angular
cd src/clients/mix-server-client
npx ng build

# Link wwwroot
cd ../..
pwsh scripts/link_wwwroot.ps1

# Run API (serves Angular app)
dotnet run --project src/api/MixServer/MixServer.csproj
```

Access at: http://localhost:5225

## Testing

### Backend Tests

```bash
dotnet test MixServer.sln
```

The backend uses:
- NUnit for test framework
- Moq for mocking
- AutoMocker for automatic dependency injection in tests
- FluentAssertions for readable assertions

### Frontend Tests

```bash
cd src/clients/mix-server-client
npm test
```

The frontend uses Karma and Jasmine for testing.

## Code Style

See [AGENTS.md](./AGENTS.md) for detailed code style guidelines.

### Quick Summary

**C# Code:**
- 4 spaces for indentation
- Private fields prefixed with `_`
- Async methods suffixed with `Async`
- Constructor parameters ordered alphabetically by type name

**TypeScript/Angular:**
- Strict mode enabled
- Use new control flow syntax (`@if`, `@for` instead of `*ngIf`, `*ngFor`)
- SCSS for component styles

## Development Scripts

All development scripts are located in the `scripts/` directory:

- **`dev-setup.ps1`** - Automated development environment setup
- **`link_wwwroot.ps1`** - Link Angular build output to .NET wwwroot
- **`regenerate_api_clients.ps1`** - Regenerate TypeScript API clients from backend
- **`migration-add.ps1`** - Add a new Entity Framework migration
- **`migration-remove.ps1`** - Remove the last Entity Framework migration

## Common Tasks

### Regenerating API Clients

After modifying any API Controllers, DTOs, or SignalR DTOs:

```powershell
pwsh scripts/regenerate_api_clients.ps1
```

This requires NSwag CLI 14.2.0 (installed automatically by the script).

### Database Migrations

**Add a new migration:**
```powershell
pwsh scripts/migration-add.ps1 -MigrationName "YourMigrationName"
```

**Remove the last migration:**
```powershell
pwsh scripts/migration-remove.ps1
```

Migrations are stored in `src/api/MixServer.Infrastructure/Migrations/`.

## Project Structure

```
mix-server/
├── src/
│   ├── api/                              # Backend .NET solution
│   │   ├── MixServer/                    # Web API project
│   │   ├── MixServer.Application/        # Application layer (Commands, Queries, DTOs)
│   │   ├── MixServer.Domain/             # Domain layer (Entities, Interfaces)
│   │   └── MixServer.Infrastructure/     # Infrastructure (EF, Repositories, Services)
│   └── clients/
│       └── mix-server-client/            # Angular frontend
├── scripts/                              # PowerShell development scripts
├── data/                                 # Local development data (gitignored)
└── Dockerfile                            # Multi-stage Docker build
```

## Architecture

Mix Server follows Clean Architecture principles:

- **MixServer** (Web): Controllers, SignalR hubs, startup configuration
- **MixServer.Application**: Command/Query handlers, DTOs, business logic
- **MixServer.Domain**: Domain entities, interfaces, settings
- **MixServer.Infrastructure**: EF Core, repositories, external services

See [AGENTS.md](./AGENTS.md) for detailed architecture documentation.

## Configuration

Configuration follows the hierarchy:
```
appsettings.json → appsettings.{Environment}.json → appsettings.Local.json
```

Key settings in `appsettings.Local.json`:
- `ConnectionStrings:DefaultConnection` - SQLite database path
- `RootFolder:Children` - Media directories (semicolon-separated)
- `HostSettings:ValidUrls` - Allowed client URLs
- `CacheSettings:Directory` - Transcode cache location
- `Ffmpeg:Path` - Path to ffmpeg binary

## Troubleshooting

### Port Already in Use

If port 5225 or 4200 is already in use, you can change the ports:

**Backend (in `src/api/MixServer/Properties/launchSettings.json`):**
```json
"applicationUrl": "http://localhost:YOUR_PORT"
```

**Frontend (in `src/clients/mix-server-client/angular.json`):**
```json
"serve": {
  "options": {
    "port": YOUR_PORT
  }
}
```

### Angular Build Fails

Try cleaning and reinstalling dependencies:
```bash
cd src/clients/mix-server-client
rm -rf node_modules package-lock.json
npm install
npx ng build
```

### .NET Build Fails

Clean and restore:
```bash
dotnet clean MixServer.sln
dotnet restore MixServer.sln
dotnet build MixServer.sln
```

### Database Issues

Delete the database and let it recreate:
```bash
rm data/mix-server.db
dotnet run --project src/api/MixServer/MixServer.csproj
```

The application will automatically apply migrations and seed the initial user.

## Getting Help

- Check [AGENTS.md](./AGENTS.md) for detailed project documentation
- Review existing issues on GitHub
- Create a new issue with the `question` label

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.
