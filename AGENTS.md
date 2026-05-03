# Repository Instructions for Codex Agents

This repository is primarily C#/.NET.

## Required workflow

Use `.codex/skills/dotnet-delivery-flow` for feature work, bug fixes, refactors, and test changes.

## General rules

- Prefer small, focused changes.
- Preserve existing architecture and naming conventions.
- Do not introduce new packages unless justified in `ARCHITECTURE.md` or the task explicitly requires it.
- For EF Core changes, check migrations, tracking behavior, query shape, and transactional boundaries.
- For ASP.NET Core changes, check validation, auth, cancellation tokens, logging, and error handling.
- Never commit secrets or local config.

## Standard verification

Run the applicable subset of:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
```

If a command is unavailable or fails due to environment setup, document the exact reason.
