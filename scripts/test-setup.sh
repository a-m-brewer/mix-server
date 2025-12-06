#!/bin/bash
# Test script for copilot-setup.sh
# This validates the setup script logic without performing actual installations

set -e

echo "========================================="
echo "Testing Copilot Setup Script"
echo "========================================="
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"

cd "$REPO_ROOT"

# Test 1: Script file exists and is executable
echo "Test 1: Checking script file..."
if [ -x ".github/copilot-setup.sh" ]; then
    echo "  [✓] Script is executable"
else
    echo "  [✗] Script is not executable"
    exit 1
fi

# Test 2: Bash syntax check
echo ""
echo "Test 2: Validating bash syntax..."
if bash -n .github/copilot-setup.sh; then
    echo "  [✓] Bash syntax is valid"
else
    echo "  [✗] Bash syntax errors found"
    exit 1
fi

# Test 3: Check for required functions
echo ""
echo "Test 3: Checking for required functions..."
if grep -q "command_exists()" .github/copilot-setup.sh; then
    echo "  [✓] command_exists function found"
else
    echo "  [✗] command_exists function not found"
    exit 1
fi

# Test 4: Check for required setup steps
echo ""
echo "Test 4: Checking for required setup steps..."
REQUIRED_STEPS=(
    "Checking .NET SDK"
    "Checking Node.js"
    "Setting up project dependencies"
    "Installing Angular dependencies"
    "Restoring .NET dependencies"
    "Building Angular client"
    "Linking wwwroot"
)

for step in "${REQUIRED_STEPS[@]}"; do
    if grep -q "$step" .github/copilot-setup.sh; then
        echo "  [✓] Found: $step"
    else
        echo "  [✗] Missing: $step"
        exit 1
    fi
done

# Test 5: Verify supporting files exist
echo ""
echo "Test 5: Checking supporting files..."
REQUIRED_FILES=(
    ".github/README.md"
    "CONTRIBUTING.md"
    "scripts/dev-setup.ps1"
    "src/api/MixServer/appsettings.Local.json.template"
    "scripts/link_wwwroot.ps1"
)

for file in "${REQUIRED_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo "  [✓] Found: $file"
    else
        echo "  [✗] Missing: $file"
        exit 1
    fi
done

# Test 6: Check prerequisites availability
echo ""
echo "Test 6: Checking current environment prerequisites..."
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

if command_exists dotnet; then
    echo "  [✓] .NET SDK: $(dotnet --version)"
else
    echo "  [!] .NET SDK not found (will be installed by setup)"
fi

if command_exists node; then
    echo "  [✓] Node.js: $(node --version)"
else
    echo "  [!] Node.js not found (will be installed by setup)"
fi

if command_exists npm; then
    echo "  [✓] npm: $(npm --version)"
else
    echo "  [!] npm not found (will be installed by setup)"
fi

if command_exists pwsh; then
    echo "  [✓] PowerShell: $(pwsh --version | head -n 1)"
else
    echo "  [!] PowerShell not found (will be installed by setup)"
fi

# Test 7: Verify project structure
echo ""
echo "Test 7: Verifying project structure..."
REQUIRED_DIRS=(
    "src/api"
    "src/clients/mix-server-client"
    "scripts"
)

for dir in "${REQUIRED_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        echo "  [✓] Found directory: $dir"
    else
        echo "  [✗] Missing directory: $dir"
        exit 1
    fi
done

# Test 8: Check if .gitignore properly excludes sensitive files
echo ""
echo "Test 8: Checking .gitignore configuration..."
if grep -q "appsettings.Local.json" .gitignore; then
    echo "  [✓] appsettings.Local.json is gitignored"
else
    echo "  [!] WARNING: appsettings.Local.json should be in .gitignore"
fi

if grep -q "data/" .gitignore; then
    echo "  [✓] data/ directory is gitignored"
else
    echo "  [!] WARNING: data/ directory should be in .gitignore"
fi

echo ""
echo "========================================="
echo "All Tests Passed!"
echo "========================================="
echo ""
echo "The copilot-setup.sh script appears to be correctly configured."
echo "To test the full setup, run: ./.github/copilot-setup.sh"
echo ""
