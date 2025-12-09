#!/bin/bash
# GitHub Copilot Agent Environment Setup
# This script runs when a Copilot agent starts to prepare the development environment
# Documentation: https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-environment

set -e

echo "========================================="
echo "Setting up Copilot Agent Environment"
echo "========================================="

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Step 1: Install .NET 10 SDK if not present
echo "Step 1: Checking .NET SDK..."
if command_exists dotnet; then
    DOTNET_VERSION=$(dotnet --version)
    echo "  [✓] .NET SDK already installed: $DOTNET_VERSION"
else
    echo "  [!] .NET SDK not found, installing..."
    
    # Detect OS and architecture
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Install .NET 10 SDK on Linux
        wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
        /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/local/share/dotnet
        
        # Add to PATH if not already there
        if [[ ":$PATH:" != *":/usr/local/share/dotnet:"* ]]; then
            export PATH="/usr/local/share/dotnet:$PATH"
            echo 'export PATH="/usr/local/share/dotnet:$PATH"' >> ~/.bashrc
        fi
        
        # Create symlink
        sudo ln -sf /usr/local/share/dotnet/dotnet /usr/local/bin/dotnet
        
        echo "  [✓] .NET SDK installed"
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        # Install .NET 10 SDK on macOS
        wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
        /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/local/share/dotnet
        
        export PATH="/usr/local/share/dotnet:$PATH"
        echo 'export PATH="/usr/local/share/dotnet:$PATH"' >> ~/.bash_profile
        
        echo "  [✓] .NET SDK installed"
    fi
    
    # Verify installation
    if command_exists dotnet; then
        DOTNET_VERSION=$(dotnet --version)
        echo "  [✓] .NET SDK version: $DOTNET_VERSION"
    else
        echo "  [✗] Failed to install .NET SDK"
        exit 1
    fi
fi

# Step 2: Install Node.js if not present (using nvm for version management)
echo ""
echo "Step 2: Checking Node.js..."
if command_exists node; then
    NODE_VERSION=$(node --version)
    echo "  [✓] Node.js already installed: $NODE_VERSION"
else
    echo "  [!] Node.js not found, installing via nvm..."
    
    # Install nvm
    curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.7/install.sh | bash
    
    # Load nvm
    export NVM_DIR="$HOME/.nvm"
    [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
    
    # Install Node.js LTS
    nvm install --lts
    nvm use --lts
    
    echo "  [✓] Node.js installed"
    
    # Verify installation
    if command_exists node; then
        NODE_VERSION=$(node --version)
        echo "  [✓] Node.js version: $NODE_VERSION"
    else
        echo "  [✗] Failed to install Node.js"
        exit 1
    fi
fi

# Verify npm is available
if command_exists npm; then
    NPM_VERSION=$(npm --version)
    echo "  [✓] npm version: $NPM_VERSION"
else
    echo "  [✗] npm not found"
    exit 1
fi

# Step 3: Install Angular CLI globally
echo ""
echo "Step 3: Checking Angular CLI..."
if command_exists ng; then
    NG_VERSION=$(ng version --skip-git 2>/dev/null | head -n 1)
    echo "  [✓] Angular CLI already installed: $NG_VERSION"
else
    echo "  [!] Angular CLI not found, installing..."
    npm install -g @angular/cli@21
    echo "  [✓] Angular CLI installed"
fi

# Step 4: Install PowerShell Core if not present
echo ""
echo "Step 4: Checking PowerShell Core..."
if command_exists pwsh; then
    PWSH_VERSION=$(pwsh --version | head -n 1)
    echo "  [✓] PowerShell Core already installed: $PWSH_VERSION"
else
    echo "  [!] PowerShell Core not found, installing..."
    
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Install PowerShell on Linux (Ubuntu/Debian)
        if command_exists apt-get; then
            # Update package list
            sudo apt-get update
            
            # Install prerequisites
            sudo apt-get install -y wget apt-transport-https software-properties-common
            
            # Get the version of Ubuntu
            source /etc/os-release
            
            # Download the Microsoft repository GPG keys
            wget -q "https://packages.microsoft.com/config/ubuntu/$VERSION_ID/packages-microsoft-prod.deb"
            
            # Register the Microsoft repository GPG keys
            sudo dpkg -i packages-microsoft-prod.deb
            
            # Delete the Microsoft repository GPG keys file
            rm packages-microsoft-prod.deb
            
            # Update the list of packages after adding packages.microsoft.com
            sudo apt-get update
            
            # Install PowerShell
            sudo apt-get install -y powershell
            
            echo "  [✓] PowerShell Core installed"
        else
            echo "  [!] Unable to install PowerShell Core automatically on this system"
        fi
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        # Install PowerShell on macOS using Homebrew
        if command_exists brew; then
            brew install --cask powershell
            echo "  [✓] PowerShell Core installed"
        else
            echo "  [!] Homebrew not found, skipping PowerShell installation"
        fi
    fi
fi

# Step 5: Install ffmpeg (optional but recommended)
echo ""
echo "Step 5: Checking ffmpeg..."
if command_exists ffmpeg; then
    FFMPEG_VERSION=$(ffmpeg -version | head -n 1 | cut -d' ' -f3)
    echo "  [✓] ffmpeg already installed: $FFMPEG_VERSION"
else
    echo "  [!] ffmpeg not found, installing..."
    
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        if command_exists apt-get; then
            sudo apt-get update
            sudo apt-get install -y ffmpeg
            echo "  [✓] ffmpeg installed"
        fi
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        if command_exists brew; then
            brew install ffmpeg
            echo "  [✓] ffmpeg installed"
        fi
    fi
fi

# Step 6: Set up project-specific dependencies
echo ""
echo "Step 6: Setting up project dependencies..."

# Create data directory structure
if [ ! -d "data" ]; then
    mkdir -p data/media
    touch data/.gitkeep
    echo "  [✓] Created data directory"
else
    echo "  [✓] Data directory already exists"
fi

# Create appsettings.Local.json if it doesn't exist
APPSETTINGS_LOCAL="src/api/MixServer/appsettings.Local.json"
if [ ! -f "$APPSETTINGS_LOCAL" ]; then
    cat > "$APPSETTINGS_LOCAL" << 'EOF'
{
  "RootFolder": {
    "Children": "../../../data/media"
  }
}
EOF
    echo "  [✓] Created appsettings.Local.json"
else
    echo "  [✓] appsettings.Local.json already exists"
fi

# Install Angular dependencies
echo ""
echo "Step 7: Installing Angular dependencies..."
if [ -d "src/clients/mix-server-client" ]; then
    cd src/clients/mix-server-client
    
    if [ -f "package.json" ]; then
        echo "  Installing npm packages (this may take a few minutes)..."
        npm ci --silent || npm install --silent
        echo "  [✓] Angular dependencies installed"
    fi
    
    cd ../../..
else
    echo "  [!] Angular client directory not found"
fi

# Restore .NET dependencies
echo ""
echo "Step 8: Restoring .NET dependencies..."
if [ -f "MixServer.sln" ]; then
    dotnet restore MixServer.sln --verbosity quiet
    echo "  [✓] .NET dependencies restored"
else
    echo "  [!] MixServer.sln not found"
fi

# Build Angular client (in background to speed up)
echo ""
echo "Step 9: Building Angular client..."
if [ -d "src/clients/mix-server-client" ]; then
    cd src/clients/mix-server-client
    
    if command_exists ng; then
        echo "  Building Angular application (this may take a few minutes)..."
        npx ng build --configuration development 2>&1 | grep -E "(Build at:|✔|ERROR)" || true
        
        if [ $? -eq 0 ]; then
            echo "  [✓] Angular client built"
        else
            echo "  [!] Angular build completed with warnings (this is normal)"
        fi
    fi
    
    cd ../../..
fi

# Link wwwroot
echo ""
echo "Step 10: Linking wwwroot..."
if [ -f "scripts/link_wwwroot.ps1" ] && command_exists pwsh; then
    pwsh scripts/link_wwwroot.ps1
    if [ $? -eq 0 ]; then
        echo "  [✓] wwwroot linked successfully"
    else
        echo "  [!] Failed to link wwwroot (will retry on first build)"
    fi
else
    echo "  [!] Skipping wwwroot linking (PowerShell not available)"
fi

echo ""
echo "========================================="
echo "Copilot Agent Environment Setup Complete"
echo "========================================="
echo ""
echo "Available tools:"
echo "  - .NET SDK: $(dotnet --version 2>/dev/null || echo 'not available')"
echo "  - Node.js: $(node --version 2>/dev/null || echo 'not available')"
echo "  - npm: $(npm --version 2>/dev/null || echo 'not available')"
echo "  - Angular CLI: $(ng version --skip-git 2>/dev/null | head -n 1 || echo 'not available')"
echo "  - PowerShell: $(pwsh --version 2>/dev/null | head -n 1 || echo 'not available')"
echo "  - ffmpeg: $(ffmpeg -version 2>/dev/null | head -n 1 | cut -d' ' -f3 || echo 'not available')"
echo ""
echo "Project ready for development!"
echo ""
