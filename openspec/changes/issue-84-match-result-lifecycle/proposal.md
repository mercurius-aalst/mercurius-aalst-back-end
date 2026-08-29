# Match result lifecycle

Issue #84 adds a server-authoritative lifecycle for match-end confirmation, score consensus, forfeits, dispute resolution, and safe reversal of a completed result. The existing administrative score route remains available for compatibility, while player and captain actions use explicit lifecycle commands.

The change persists the lifecycle state and the participant submissions needed to enforce deadlines and consensus. All transitions are validated in the tournament module, execute transactionally with bracket propagation/reversal, and are exposed through privacy-safe match projections. Authentication and role checks remain at the API boundary and participant ownership is revalidated in the service. The current platform contract has a global admin role rather than a per-tournament assignment relation, so a global admin is the assigned tournament authority for this change; a future assignment model MUST return admin_not_assigned for other admins instead of widening authority implicitly.

## Non-goals

- No decline action is introduced; the contract supports confirmation only.
- No client-owned timers or bracket advancement are introduced.
- Public match projections do not expose authenticated subject identifiers or private moderation notes.
