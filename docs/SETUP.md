# Development Environment Setup - Quick Reference

This document provides a quick reference for setting up the Mix Server development environment.

## For GitHub Copilot Agents

**Automatic Setup**: The environment is automatically configured when an agent starts.

- **Setup Script**: `.github/copilot-setup.sh`
- **What it does**: Installs .NET SDK, Node.js, Angular CLI, PowerShell, ffmpeg, creates directories, installs dependencies
- **No action needed**: Just start working on your assigned task

## For Local Development

### Quick Start (Automated)

**Windows/Linux/macOS with PowerShell:**
```powershell
pwsh scripts/dev-setup.ps1
```

This script will:
1. ✓ Check prerequisites (.NET, Node.js, npm, PowerShell)
2. ✓ Create `data/` and `data/media/` directories
3. ✓ Create `appsettings.Local.json` configuration
4. ✓ Install Angular dependencies
5. ✓ Build Angular client
6. ✓ Link wwwroot to Angular output
7. ✓ Restore .NET dependencies

### Manual Setup

See [CONTRIBUTING.md](../CONTRIBUTING.md) for detailed manual setup instructions.

### Prerequisites

- **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js LTS (22+)** - [Download](https://nodejs.org/)
- **PowerShell Core** - [Download](https://github.com/PowerShell/PowerShell/releases)
- **ffmpeg** (optional) - [Download](https://ffmpeg.org/download.html)

## Running the Application

### Development Mode (with live reload)

**Terminal 1 - Backend:**
```bash
dotnet watch --project src/api/MixServer/MixServer.csproj
```

**Terminal 2 - Frontend:**
```bash
cd src/clients/mix-server-client
npx ng serve
```

**Access:**
- Angular UI: http://localhost:4200
- API: http://localhost:5225
- API Swagger: http://localhost:5225/swagger

### Production Mode

```bash
# Build everything
pwsh scripts/dev-setup.ps1

# Run (serves Angular from API)
dotnet run --project src/api/MixServer/MixServer.csproj
```

**Access:** http://localhost:5225

## Common Commands

### Backend (.NET)

```bash
# Build solution
dotnet build MixServer.sln

# Run tests
dotnet test MixServer.sln

# Run API
dotnet run --project src/api/MixServer/MixServer.csproj

# Watch mode (auto-reload)
dotnet watch --project src/api/MixServer/MixServer.csproj
```

### Frontend (Angular)

```bash
cd src/clients/mix-server-client

# Install dependencies
npm ci --legacy-peer-deps

# Build
npx ng build

# Serve (development)
npx ng serve

# Run tests
npm test
```

### Development Scripts

```powershell
# Automated setup
pwsh scripts/dev-setup.ps1

# Link Angular build to .NET wwwroot
pwsh scripts/link_wwwroot.ps1

# Regenerate API clients (after API changes)
pwsh scripts/regenerate_api_clients.ps1

# Database migrations
pwsh scripts/migration-add.ps1 -MigrationName "YourName"
pwsh scripts/migration-remove.ps1
```

## Configuration Files

### Created During Setup

- `data/` - Local development data directory (gitignored)
  - `data/media/` - Place audio files here for testing
  - `data/mix-server.db` - SQLite database (auto-created)
- `src/api/MixServer/appsettings.Local.json` - Local settings (gitignored)

### Templates

- `src/api/MixServer/appsettings.Local.json.template` - Configuration template

### Example Configuration

**appsettings.Local.json:**
```json
{
  "RootFolder": {
    "Children": "../../../data/media"
  }
}
```

## Testing the Setup

### Quick Validation

```bash
# Run automated tests
bash scripts/test-setup.sh
```

### Manual Verification

```bash
# Check tools are installed
dotnet --version
node --version
npm --version
pwsh --version

# Verify project builds
dotnet build MixServer.sln

# Verify Angular builds
cd src/clients/mix-server-client && npx ng build
```

## Troubleshooting

### Port Already in Use

**Backend (5225):**
Edit `src/api/MixServer/Properties/launchSettings.json`

**Frontend (4200):**
Edit `src/clients/mix-server-client/angular.json`

### Angular Build Fails

```bash
cd src/clients/mix-server-client
rm -rf node_modules package-lock.json
npm install --legacy-peer-deps
npx ng build
```

### .NET Build Fails

```bash
dotnet clean MixServer.sln
dotnet restore MixServer.sln
dotnet build MixServer.sln
```

### Database Issues

```bash
# Delete and recreate database
rm data/mix-server.db
dotnet run --project src/api/MixServer/MixServer.csproj
```

## Project Structure

```
mix-server/
├── .github/
│   ├── copilot-setup.sh          # Copilot agent environment setup
│   └── workflows/                 # CI/CD pipelines
├── src/
│   ├── api/                       # .NET backend
│   │   ├── MixServer/             # Web API
│   │   ├── MixServer.Application/ # Business logic
│   │   ├── MixServer.Domain/      # Domain models
│   │   └── MixServer.Infrastructure/ # Data access
│   └── clients/
│       └── mix-server-client/     # Angular frontend
├── scripts/
│   ├── dev-setup.ps1              # Local development setup
│   ├── link_wwwroot.ps1           # Link Angular to .NET
│   ├── regenerate_api_clients.ps1 # Generate TypeScript clients
│   └── test-setup.sh              # Validate setup
├── data/                          # Local data (gitignored)
├── CONTRIBUTING.md                # Detailed setup guide
└── AGENTS.md                      # Project documentation
```

## Additional Resources

- **[CONTRIBUTING.md](../CONTRIBUTING.md)** - Detailed contribution guide
- **[AGENTS.md](../AGENTS.md)** - Complete project documentation
- **[.github/README.md](../.github/README.md)** - GitHub configuration docs
- **[README.md](../README.md)** - Project overview and Docker setup

## Getting Help

1. Check the troubleshooting section above
2. Review [CONTRIBUTING.md](../CONTRIBUTING.md)
3. Check existing GitHub issues
4. Create a new issue with the `question` label
