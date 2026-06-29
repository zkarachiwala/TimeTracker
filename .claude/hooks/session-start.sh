#!/bin/bash
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Install .NET 10 via apt if not already present
if ! command -v dotnet &> /dev/null || [[ "$(dotnet --version 2>/dev/null)" != 10.* ]]; then
  echo "Installing .NET 10 SDK..."
  # Add the Microsoft package feed (required for dotnet-sdk-10.0 on Ubuntu 24.04)
  . /etc/os-release
  curl -fsSL "https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb" \
    -o /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  rm /tmp/packages-microsoft-prod.deb
  apt-get update -qq 2>/dev/null || true
  apt-get install -y dotnet-sdk-10.0 -qq
fi

echo ".NET version: $(dotnet --version)"

# Restore NuGet packages
echo "Restoring NuGet packages..."
dotnet restore "$CLAUDE_PROJECT_DIR/TimeTracker.sln" --nologo --verbosity minimal

# Build solution
echo "Building solution..."
dotnet build "$CLAUDE_PROJECT_DIR/TimeTracker.sln" --no-restore --configuration Release --nologo --verbosity minimal

echo "Session setup complete."
