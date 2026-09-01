# Match result lifecycle

Issue #84 adds a server-authoritative lifecycle for match-end confirmation, score consensus, forfeits, dispute resolution, and safe reversal of a completed result. The existing administrative score route remains available for compatibility, while player and captain actions use explicit lifecycle commands.

The change persists the lifecycle state and the participant submissions needed to enforce deadlines and consensus. All transitions are validated in the tournament module, execute transactionally with bracket propagation/reversal, and are exposed through privacy-safe match projections. Authentication and role checks remain at the API boundary and participant ownership is revalidated in the service. An optional tournament admin assignment selects the primary recipient for resolution-required notifications and the administrator who may inspect private reports; it MUST NOT restrict other authenticated global administrators from resolving, force-forfeiting, or reversing when the authoritative lifecycle and bracket rules allow the action. No reassignment prerequisite exists, and lifecycle commands MUST NOT return admin_not_assigned for an otherwise authorized global administrator.

## Non-goals

- No decline action is introduced; the contract supports confirmation only.
- No client-owned timers or bracket advancement are introduced.
- Public match projections do not expose authenticated subject identifiers or private moderation notes.
