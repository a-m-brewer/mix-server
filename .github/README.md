# GitHub Configuration

This directory contains GitHub-specific configuration files for the Mix Server project.

## Files

### `copilot-setup.sh`

This script automatically sets up the development environment for GitHub Copilot Agents. When a Copilot agent starts working on this repository, this script runs to ensure all necessary tools and dependencies are available.

**What it does:**
- Installs .NET 10 SDK if not present
- Installs Node.js LTS if not present
- Installs Angular CLI globally
- Installs PowerShell Core if not present
- Installs ffmpeg (optional, for transcoding features)
- Creates the `data/` directory structure
- Creates `appsettings.Local.json` with default configuration
- Installs Angular dependencies
- Restores .NET dependencies
- Builds the Angular client
- Links the wwwroot directory to the Angular build output

**Documentation:**
- [Customizing Copilot Agent Environment](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-environment)
- [Preinstalling Tools or Dependencies](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-environment#preinstalling-tools-or-dependencies-in-copilots-environment)

**Manual Testing:**

To test the setup script manually:

```bash
./.github/copilot-setup.sh
```

**Note:** This script is designed to be idempotent - it can be run multiple times safely and will only install missing components.

### `workflows/`

Contains GitHub Actions workflow definitions for CI/CD pipelines.

#### `docker-image.yml`

Builds multi-architecture Docker images (amd64, arm64) and publishes them to:
- Docker Hub: `adammbrewer/mix-server`
- GitHub Container Registry: `ghcr.io/a-m-brewer/mix-server`

Triggered on:
- Push to `main` branch
- Tags matching `*.*.*`
- Pull requests to `main`

## Development

For local development setup, see:
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Manual setup instructions
- `scripts/dev-setup.ps1` - Automated setup script for local development

## Copilot Agent Usage

When a GitHub Copilot agent is assigned to work on this repository, it will automatically:

1. Clone the repository
2. Execute `.github/copilot-setup.sh`
3. Have a fully configured environment ready to:
   - Build the .NET solution
   - Build the Angular frontend
   - Run tests
   - Make code changes
   - Test changes locally

This ensures agents can immediately start working on features, bug fixes, or other tasks without manual intervention.

## Environment Variables

The Copilot setup script respects the following environment variables:

- `PATH` - Automatically updated to include installed tools
- `NVM_DIR` - Node Version Manager directory (if nvm is used)
- Standard .NET and Node.js environment variables

## Troubleshooting

If the Copilot setup script fails:

1. Check the agent's console output for specific error messages
2. Verify the script has execute permissions: `chmod +x .github/copilot-setup.sh`
3. Test the script manually in a clean environment
4. Check if the required tools can be downloaded (network issues, permissions)

Common issues:
- **Permission denied**: The script needs sudo access for some installations on Linux
- **Network timeouts**: Installing tools requires internet access
- **Disk space**: Ensure adequate disk space for .NET SDK, Node.js, and npm packages

## Maintenance

When updating tool versions:

1. Update `.github/copilot-setup.sh` with new version numbers or URLs
2. Update `AGENTS.md` to reflect the new versions in the Tech Stack section
3. Update `CONTRIBUTING.md` prerequisites section
4. Test the setup script in a clean environment
5. Update any Docker images or CI/CD workflows that depend on specific versions
