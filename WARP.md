# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Mix Server is a web-based audio player for DJ mixes and long audio files, featuring progress tracking, file explorer UI, multi-device control, and user management. It's built with ASP.NET Core 8.0 and Angular 18, designed for deployment via Docker.

## Development Commands

### Backend (.NET/API)

**Build and Run:**
```bash
# Build the solution
dotnet build MixServer.sln

# Run the main project
dotnet run --project src/api/MixServer

# Run tests
dotnet test src/api/Tests/MixServer.Infrastructure.Tests/MixServer.Infrastructure.Tests.csproj
```

**Database Migrations:**
```bash
# Add a new migration (from repository root)
pwsh scripts/migration-add.ps1 -MigrationName "YourMigrationName"

# Remove the last migration (from repository root)
pwsh scripts/migration-remove.ps1
```

### Frontend (Angular)

**Setup and Development:**
```bash
# Navigate to client directory
cd src/clients/mix-server-client

# Install dependencies
npm install

# Build the client
ng build

# Serve with hot reload
ng serve

# Run tests
ng test
```

**Development Setup:**
After building the Angular client, run the linking script to connect the frontend to the API:
```bash
# From repository root
pwsh scripts/link_wwwroot.ps1
```

### API Client Regeneration

When making changes to API controllers, DTOs, or SignalR DTOs:
```bash
pwsh scripts/regenerate_api_clients.ps1
```

## Architecture

### Clean Architecture Pattern
The project follows Clean Architecture with clear separation of concerns:

- **MixServer** - Web API layer (Controllers, SignalR hubs, authentication)
- **MixServer.Application** - Application services and CQRS commands/queries
- **MixServer.Domain** - Domain models, entities, and business logic
- **MixServer.Infrastructure** - Data access, Entity Framework, external services

### Key Domain Areas

**FileExplorer** - Core file system browsing and metadata management
- Handles folder scanning, file indexing, and media metadata extraction
- File nodes represent both files and folders with support for transcoding

**Sessions** - Playback session management
- Tracks user progress through audio files
- Supports multi-device playback synchronization
- Maintains playback history

**Users** - User authentication and management
- JWT-based authentication
- Role-based authorization (Admin/User)
- User-specific progress and settings

**Streams** - Audio transcoding and streaming
- FFmpeg-based transcoding to HLS format
- Background processing for transcode requests
- Caching of transcoded segments

**Queueing** - Playlist and queue management
- Audio file queuing across folders
- Queue state persistence per user

### Background Services
The application runs several background services for:
- Folder scanning and file indexing
- File metadata extraction and updates
- Audio transcoding
- Device detection
- File system watching

## Configuration

### Required Local Configuration
Create `src/api/MixServer/appsettings.Local.json`:
```json
{
    "RootFolder": {
        "Children": "./media"
    }
}
```

Create a `data/` directory in the repository root for SQLite database storage.

### Environment Variables
Use the prefix `MIX_SERVER_` for production environment variables:
- `MIX_SERVER_RootFolder__Children` - Media directories (semicolon separated)
- `MIX_SERVER_HostSettings__ValidUrls` - Valid client URLs
- `ASPNETCORE_URLS` - Server binding URL
- `DOTNET_ENVIRONMENT` - Environment (Development/Production)

## Development Workflow

1. **Backend Development:**
   - Start with domain models in `MixServer.Domain`
   - Add application services in `MixServer.Application` using CQRS pattern
   - Implement repositories in `MixServer.Infrastructure`
   - Add controllers and SignalR hubs in `MixServer`

2. **Frontend Development:**
   - Angular client is served from `http://localhost:4200` during development
   - API runs on `http://localhost:5225`
   - Use `ng serve` for hot reload during frontend development

3. **Full Stack Changes:**
   - Run `scripts/regenerate_api_clients.ps1` after API changes
   - Use `scripts/link_wwwroot.ps1` for production builds
   - Build frontend with `ng build` before running API in production mode

## Testing

### Backend Tests
- Located in `src/api/Tests/MixServer.Infrastructure.Tests`
- Focuses on infrastructure layer and Entity Framework integration
- Run with `dotnet test`

### Frontend Tests
- Jasmine/Karma-based testing
- Run with `ng test` from the client directory

## Docker Development

The project includes a multi-stage Dockerfile for containerized deployment:
```bash
# Build the Docker image
docker build -t mix-server .

# Run with docker-compose (see README.md for full example)
docker-compose up
```

## Key Technologies

- **Backend:** ASP.NET Core 8.0, Entity Framework Core, SignalR, FluentValidation, MediatR pattern
- **Frontend:** Angular 18, Angular Material, TypeScript, SignalR client
- **Database:** SQLite (with EF Core migrations)
- **Audio Processing:** FFmpeg for transcoding to HLS format
- **Authentication:** JWT with ASP.NET Core Identity
- **API Documentation:** NSwag for client generation

## File Structure Notes

- `src/api/` - All backend code
- `src/clients/mix-server-client/` - Angular frontend
- `scripts/` - PowerShell utility scripts
- `data/` - SQLite database location (create locally)
- `screenshots/` - UI screenshots for documentation
