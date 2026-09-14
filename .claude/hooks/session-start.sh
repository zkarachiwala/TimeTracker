#!/bin/bash
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Install the exact SDK global.json pins, pulled from Microsoft Container
# Registry (works around apt's frozen 1xx feature band and the sandbox
# network policy blocking Microsoft's SDK CDN directly - see
# .claude/hooks/install-dotnet-sdk.py for the full explanation).
python3 "$CLAUDE_PROJECT_DIR/.claude/hooks/install-dotnet-sdk.py" "$CLAUDE_PROJECT_DIR"

echo ".NET version: $(dotnet --version)"

# Restore NuGet packages
echo "Restoring NuGet packages..."
dotnet restore "$CLAUDE_PROJECT_DIR/TimeTracker.sln" --nologo --verbosity minimal

# Build solution
echo "Building solution..."
dotnet build "$CLAUDE_PROJECT_DIR/TimeTracker.sln" --no-restore --configuration Release --nologo --verbosity minimal

echo "Session setup complete."
