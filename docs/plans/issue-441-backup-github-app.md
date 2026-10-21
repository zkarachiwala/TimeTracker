# Issue #441 — Backup push: replace expiring PAT with a GitHub App

**Branch:** `fix/441-backup-github-app`

## Root cause
`BACKUP_REPO_TOKEN` (fine-grained PAT, set 2026-07-15) expired after 90 days. Last good backup 2026-10-13; every nightly run since fails at `git clone` with "Invalid username or token".

## Fix
Swap the PAT for a GitHub App installation token minted per run by `actions/create-github-app-token@v3`:
1-hour lifetime, revoked at job end, scoped to `TimeTracker-backups` with `contents: write` only. The app's private key does not expire.

## Steps
| # | Who | Step | Status |
|---|-----|------|--------|
| 1 | Zak | Register GitHub App (`Settings → Developer settings → GitHub Apps → New`) — Contents: Read & write, no webhook, only this account | ⬜ |
| 2 | Zak | Note the **Client ID**; generate + download a **private key** (.pem) | ⬜ |
| 3 | Zak | Install the app on `TimeTracker-backups` only | ⬜ |
| 4 | Zak | `gh variable set BACKUP_APP_CLIENT_ID` + `gh secret set BACKUP_APP_PRIVATE_KEY < key.pem`; then delete the local .pem | ⬜ |
| 5 | Claude | Update `backup.yml` push step to use the app token | ✅ |
| 6 | Claude | Update `docs/azure-deployment.md` (Step G/H), ADR-021 amendment + new ADR-039 | ✅ |
| 7 | Zak/Claude | Verify: `gh workflow run backup.yml --ref fix/441-backup-github-app`; confirm a commit lands in `TimeTracker-backups` | ⬜ |
| 8 | Claude | Commit, push, PR (`Closes #441`) | ⬜ |
| 9 | Zak | After merge: delete `BACKUP_REPO_TOKEN` secret and revoke the old PAT | ⬜ |
