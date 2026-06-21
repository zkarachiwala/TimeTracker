#!/bin/bash
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Install .NET 10 via apt if not already present
if ! command -v dotnet &> /dev/null || [[ "$(dotnet --version 2>/dev/null)" != 10.* ]]; then
  echo "Installing .NET 10 SDK..."
  apt-get update --allow-unauthenticated \
    -o Acquire::AllowInsecureRepositories=true \
    -o Dir::Etc::sourcelist="sources.list" \
    -o Dir::Etc::sourceparts="-" \
    -qq 2>/dev/null || true
  apt-get install -y dotnet-sdk-10.0 --allow-unauthenticated -qq
fi

echo ".NET version: $(dotnet --version)"

# Restore NuGet packages
echo "Restoring NuGet packages..."
dotnet restore "$CLAUDE_PROJECT_DIR/TimeTracker.sln" --nologo --verbosity minimal

# Build solution
echo "Building solution..."
dotnet build "$CLAUDE_PROJECT_DIR/TimeTracker.sln" --no-restore --configuration Release --nologo --verbosity minimal

echo "Session setup complete."
