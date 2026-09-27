# Restrict public user profile details

Public user profile and team responses currently expose profile details and platform identifiers. Public views should expose usernames only, while detailed profile access belongs to administrators and to the two opposing individuals or team captains in an assigned tournament match.

## Scope

- Limit public user profile and public team member responses to usernames.
- Add an administrator-only user lookup by username for the existing detailed user profile page.
- Restrict user-specific match summaries to administrators while keeping public team match summaries available.
- Add a match-scoped opponent profile response for individuals and team captains, authorized from the assigned match itself.
- Cover anonymous, unrelated authenticated, team-member, captain, participant, and administrator access boundaries.
