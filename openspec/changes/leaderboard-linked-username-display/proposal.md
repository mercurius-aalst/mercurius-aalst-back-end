# Display linked LAN users by username in leaderboards

Linked tournament participants are currently displayed using their full profile name. Tournament leaderboards should identify LAN users by their public username, consistently in live rankings, administrator result entry, and final placements. Guest participants must keep the display name entered for them.

This change defines and implements that presentation rule without changing the leaderboard response shape or adding read-time identity lookups. The public username is snapshotted once, when a new linked participant is created, and stored in the participant's existing display name; every read projection then shows that stored value. If the linked user has no public username — for example an incomplete or otherwise non-public profile — a generic privacy-safe placeholder is stored instead, so a real name is never exposed on the anonymous leaderboard.

## Scope

- Resolve the public username once at participant creation and store it in the existing display name.
- Fall back to the generic "Incomplete profile" placeholder when no public username is available, never to a real profile name.
- Keep guest display names exactly as entered.
- Rely on the stored display name in public leaderboard, administrator participant, and finalized placement projections.
- Add regression coverage for the create-time snapshot and its transactional guarantee.
