#!/usr/bin/env bash
set -euo pipefail

if ! grep -q "Verdict: APPROVED" REVIEW.md; then
  echo "Reviewer has not approved. PR creation aborted." >&2
  exit 1
fi

TITLE=$(awk '/^7\. PR title/{flag=1;next}/^8\. PR description/{flag=0}flag' REVIEW.md | sed '/^$/d' | head -n 1)
if [ -z "${TITLE}" ]; then
  TITLE="Codex changes"
fi

awk '/^8\. PR description/{flag=1;next}flag' REVIEW.md > PR_DESCRIPTION.md

gh pr create --title "$TITLE" --body-file PR_DESCRIPTION.md
