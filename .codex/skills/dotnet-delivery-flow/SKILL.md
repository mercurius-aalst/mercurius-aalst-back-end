---
name: dotnet-delivery-flow
description: End-to-end multi-agent delivery flow for C#/.NET GitHub repositories: analysis, architecture, implementation, tests, read-only pre-PR review, and PR creation.
---

# .NET Delivery Flow

Use this skill for C#/.NET feature work, bug fixes, refactors, and test changes.

## Goals

- Keep expensive reasoning where it matters.
- Use plan mode before implementation.
- Keep implementation and tests in logical commits.
- Make Reviewer read-only.
- Create a PR only after Reviewer outputs `APPROVED`.

## Agent sequence

### 1. Analyst

Agent: `analyst`

Mode: plan/read-only.

Tasks:
- Read the request, issue, README, AGENTS.md, solution structure, and relevant code.
- Produce `ANALYSIS.md`.
- Do not modify source code.

Exit criteria:
- `ANALYSIS.md` has clear acceptance criteria, scope, non-scope, risks, and impacted areas.

### 2. Architect

Agent: `architect`

Mode: plan/read-only.

Tasks:
- Read `ANALYSIS.md` and relevant code.
- Produce `ARCHITECTURE.md`.
- Decide whether implementation should use `developer-senior` or `developer-fast`.

Routing rule:
- Use `developer-senior` for domain logic, multi-file changes, EF Core, security, concurrency, refactors, or unclear impact.
- Use `developer-fast` for isolated, mechanical, low-risk changes.

Exit criteria:
- `ARCHITECTURE.md` includes implementation steps, test strategy, and routing.

### 3. Developer

Agent: `developer-senior` or `developer-fast` based on `ARCHITECTURE.md`.

Mode: implementation/workspace-write.

Tasks:
- Implement only the planned scope.
- Make small logical commits.
- Run relevant build/test commands.
- Produce `DEVELOPMENT_NOTES.md`.

Commit guidance:
- Commit per coherent slice.
- Keep refactors separate from behavior changes.
- Do not commit failing work unless explicitly documenting a handoff/blocker.

### 4. Tester

Agent: `tester`

Mode: test implementation/workspace-write.

Tasks:
- Add or update tests for acceptance criteria and edge cases.
- Make test-only commits.
- Run targeted tests and broader tests when practical.
- Produce `TEST_NOTES.md`.

Routing rule:
- If production code is wrong, route back to Developer.
- If tests are wrong or incomplete, fix tests.

### 5. Reviewer

Agent: `reviewer`

Mode: read-only review.

Tasks:
- Review requirements, architecture, code, commits, and tests.
- Produce `REVIEW.md`.
- Never modify files.

If verdict is `CHANGES_REQUESTED`:
- Route implementation issues to Developer.
- Route missing/faulty tests to Tester.
- Repeat Tester/Reviewer or Developer/Tester/Reviewer as needed.

If verdict is `APPROVED`:
- Create `PR_DESCRIPTION.md` from REVIEW.md.
- Create the PR using the GitHub MCP or `gh` CLI.

## PR description template

Use this structure:

```md
## Summary

- 

## Technical changes

- 

## Tests

- 

## Risks / migration notes

- 

## Review status

Pre-PR review: APPROVED
```

## Recommended commands

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
```

Use targeted commands when the repository is large.

## PR creation

Preferred if GitHub CLI is available:

```bash
gh pr create --title "<title from REVIEW.md>" --body-file PR_DESCRIPTION.md
```

If GitHub MCP is configured, use it to create the PR with the same title/body.
