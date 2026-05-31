#!/usr/bin/env bash
# Smoke test: build, start app, curl all routes, report 500s.
# Usage: ./scripts/smoke-test.sh
# Requires: dotnet, curl. App must not already be running on port 5019.

set -euo pipefail

APP_URL="http://localhost:5019"
PROJECT="TimeTracker.Web"
PASS=0
FAIL=0
ERRORS=()

echo "=== Build ==="
dotnet build TimeTracker.Web/TimeTracker.Web.csproj --nologo -q
echo "Build OK"

echo ""
echo "=== Starting app ==="
cd "$PROJECT"
dotnet run --no-build --launch-profile http &
APP_PID=$!
cd ..

# Wait for app to be ready (up to 30s)
echo -n "Waiting for app"
for i in $(seq 1 30); do
  if curl -s -o /dev/null "$APP_URL/login" 2>/dev/null; then
    echo " ready."
    break
  fi
  echo -n "."
  sleep 1
done

echo ""
echo "=== Route smoke test ==="

check() {
  local route="$1"
  local status
  status=$(curl -s -o /dev/null -w "%{http_code}" --max-redirs 0 "$APP_URL$route" 2>/dev/null) || true
  if [[ "$status" == 5* ]]; then
    echo "  FAIL  $route → $status"
    ERRORS+=("$route → HTTP $status")
    FAIL=$((FAIL + 1))
  else
    echo "  OK    $route → $status"
    PASS=$((PASS + 1))
  fi
}

# Public routes (no auth)
check "/login"      "2xx"

# Auth-gated routes — expect redirect (302/301) not 500
check "/"           "3xx"
check "/entries"    "3xx"
check "/reports"    "3xx"
check "/projects"   "3xx"
check "/clients"    "3xx"

echo ""
echo "=== Stopping app ==="
kill "$APP_PID" 2>/dev/null || true
wait "$APP_PID" 2>/dev/null || true

echo ""
echo "=== Results ==="
echo "  Passed: $PASS  Failed: $FAIL"
if [ ${#ERRORS[@]} -gt 0 ]; then
  echo ""
  echo "FAILURES:"
  for e in "${ERRORS[@]}"; do echo "  - $e"; done
  exit 1
else
  echo "All routes OK."
fi
