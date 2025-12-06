# AGENTS.md

## Project Overview
Mix Server is a web-based audio player designed for listening to long audio files (DJ mixes) with progress tracking. It uses a file explorer-based UI and supports multi-device playback control.

**Tech Stack:**
- Backend: ASP.NET Core 10.0 (C#)
- Frontend: Angular 21 with Angular Material
- Database: SQLite with Entity Framework Core
- Real-time: SignalR for multi-device sync
- Media: ffmpeg for audio transcoding (HLS streaming)

## Project Structure
```
mix-server/
├── .github/
│   ├── copilot-setup.sh                  # GitHub Copilot agent environment setup
│   ├── README.md                          # GitHub configuration documentation
│   └── workflows/                         # GitHub Actions CI/CD workflows
├── src/
│   ├── api/                              # Backend .NET solution
│   │   ├── MixServer/                    # Web API project (Controllers, SignalR hubs)
│   │   ├── MixServer.Application/        # Application layer (Commands, Queries, DTOs)
│   │   ├── MixServer.Domain/             # Domain layer (Entities, Interfaces, Settings)
│   │   └── MixServer.Infrastructure/     # Infrastructure (EF, Repositories, Services)
│   └── clients/
│       └── mix-server-client/            # Angular frontend
│           └── src/app/
│               ├── generated-clients/    # Auto-generated API clients (NSwag)
│               ├── services/             # Angular services
│               └── components/           # UI components
├── scripts/                              # PowerShell development scripts
├── data/                                 # Local development data directory
├── CONTRIBUTING.md                       # Development setup and contribution guide
└── Dockerfile                            # Multi-stage Docker build
```

## Build Commands

### Prerequisites
- .NET 10.0 SDK
- Node.js LTS (22+)
- PowerShell Core (pwsh)
- ffmpeg (for transcoding features)

### Backend (.NET)
```powershell
# Build solution
dotnet build MixServer.sln

# Run API (from repo root)
dotnet run --project src/api/MixServer/MixServer.csproj

# Run in watch mode
dotnet watch --project src/api/MixServer/MixServer.csproj
```

### Frontend (Angular)
```powershell
# Navigate to client directory
cd src/clients/mix-server-client

# Install dependencies
npm ci

# Build
npx ng build

# Serve (development)
npx ng serve
```

### Automated Development Setup

**For GitHub Copilot Agents:**
The environment is automatically configured when an agent starts. The `.github/copilot-setup.sh` script installs all prerequisites and sets up the development environment.

**For Local Development:**
Use the automated setup script:
```powershell
pwsh scripts/dev-setup.ps1
```

**Manual Setup:**
1. Build Angular client: `cd src/clients/mix-server-client && npm ci && npx ng build`
2. Link wwwroot: `pwsh scripts/link_wwwroot.ps1`
3. Create `data/` directory in repo root
4. Create `src/api/MixServer/appsettings.Local.json`:
   ```json
   {
       "RootFolder": {
           "Children": "./media"
       }
   }
   ```
5. Run API: `dotnet run --project src/api/MixServer/MixServer.csproj`
6. Access: Angular UI at `http://localhost:4200`, API at `http://localhost:5225`

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed setup instructions.

### Docker
```powershell
# Build image
docker build -t mix-server .

# Run container
docker run -p 5225:5225 mix-server
```

## Testing

### Backend Tests
```powershell
dotnet test MixServer.sln
```
Testing uses NUnit, Moq, AutoMocker, and FluentAssertions.

### Frontend Tests
```powershell
cd src/clients/mix-server-client
npm test
```
Angular tests use Karma and Jasmine.

## Code Style

### C# Code Style

**Indentation:** 4 spaces (no tabs)

**Naming:**
- Private fields: prefix with underscore (`_fieldName`), readonly first alphabetically, then regular fields
- Async methods: suffix with `Async`
- No public fields - use properties instead

**Constructors (DI):**
- Parameters ordered alphabetically by type name
- Parameter name = lowerCamelCase of interface without `I` prefix
```csharp path=src/api/MixServer.Application/Queueing/Commands/AddToQueue/AddToQueueCommandHandler.cs start=11
public class AddToQueueCommandHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> converter,
    IFileService fileService,
    IQueueService queueService,
    INodePathDtoConverter nodePathDtoConverter,
    IValidator<AddToQueueCommand> validator)
    : ICommandHandler<AddToQueueCommand, QueueSnapshotDto>
```

**Method Chaining:**
Split at `.` character onto new lines from the second `.`:
```csharp path=src/api/MixServer.Infrastructure/Queueing/Models/UserQueue.cs start=162
var finalQueue = UserQueueSortItems
    .Where(w => !w.PreviousFolderItemId.HasValue)
    .Select(s => s)
    .Cast<QueueSortItem>()
    .ToList();
```

For fluent APIs (FluentValidation, test mocks), break before first `.`:
```csharp path=src/api/MixServer.Application/Users/Commands/LoginUser/LoginUserCommandValidator.cs start=9
RuleFor(r => r.Username)
    .NotEmpty();

RuleFor(r => r.Password)
    .NotEmpty();
```

**Unit Tests:**
- Naming: `MethodName_Scenario_Expectation`
- Required section comments:
```csharp path=null start=null
// Arrange
var testFile = new FileExplorerFileNode(new NodePath("/media", "test.mp3"));

// Act
await Subject.AddToQueueAsync(testFile, CancellationToken.None);

// Assert
GetMock<IQueueService>()
    .Verify(x => x.AddToQueueAsync(testFile, It.IsAny<CancellationToken>()), Times.Once);
```

### TypeScript/Angular Code Style
- Strict mode enabled
- SCSS for component styles
- Components use `.component.ts` suffix
- Services use `.service.ts` suffix

## API Client Generation
When modifying API Controllers, DTOs, or SignalR DTOs, regenerate the TypeScript clients:
```powershell
pwsh scripts/regenerate_api_clients.ps1
```
This requires nswag CLI 14.2.0 (installed automatically by script).

## Database Migrations
```powershell
# Add migration
pwsh scripts/migration-add.ps1 -MigrationName "YourMigrationName"

# Remove last migration
pwsh scripts/migration-remove.ps1
```
Migrations target SQLite and are located in `src/api/MixServer.Infrastructure/Migrations/`.

## Architecture Notes

### Backend Architecture (Clean Architecture)
- **MixServer** (Web): Controllers, SignalR hubs, startup configuration
- **MixServer.Application**: Command/Query handlers, DTOs, business logic orchestration
- **MixServer.Domain**: Domain entities, interfaces, settings, utilities
- **MixServer.Infrastructure**: EF DbContext, repositories, external services, file system operations

### Command/Query Pattern
Controllers use `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TResult>` for operations:
```csharp
public class QueueController(
    ICommandHandler<AddToQueueCommand, QueueSnapshotDto> addToQueueCommandHandler,
    IQueryHandler<QueueSnapshotDto> getCurrentQueueQueryHandler)
```

### SignalR
Real-time communication for multi-device sync located in `src/api/MixServer/SignalR/`.

## Environment Configuration
Configuration hierarchy: `appsettings.json` → `appsettings.{Environment}.json` → `appsettings.Local.json`

Key settings:
- `ConnectionStrings:DefaultConnection` - SQLite database path
- `RootFolder:Children` - Media directories (semicolon-separated)
- `HostSettings:ValidUrls` - Allowed client URLs
- `CacheSettings:Directory` - Transcode cache location
- `Ffmpeg:Path` - Path to ffmpeg binary

## CI/CD
GitHub Actions workflow (`.github/workflows/docker-image.yml`) builds multi-arch Docker images (amd64, arm64) on:
- Push to `main`
- Tags matching `*.*.*`
- Pull requests to `main`

Images published to Docker Hub (`adammbrewer/mix-server`) and GHCR (`ghcr.io/a-m-brewer/mix-server`).
