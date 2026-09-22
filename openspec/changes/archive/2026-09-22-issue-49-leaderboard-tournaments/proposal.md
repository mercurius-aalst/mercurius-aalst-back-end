# Leaderboard tournaments

Issue #49 adds an individual leaderboard tournament type for competitions ranked by a participant's best score or elapsed time. Leaderboard participants are established by administrative result entry and may be linked LAN users or standalone guests, so registration and generated matches do not participate in this flow.

The change adds metric-aware tournament configuration, persisted participant and attempt history, deterministic public ranking, administrator-only result management, lifecycle integration, final guest-capable placements, and reset behavior. Existing match-based tournament paths remain unchanged.

## Non-goals

- No public or self-service leaderboard registration.
- No head-to-head matches, brackets, or match scheduling for leaderboard tournaments.
- No display-time rounding in persisted score or duration values.
