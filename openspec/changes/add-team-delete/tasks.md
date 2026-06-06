# Tasks

- [x] 1.1 Add soft-delete state to the `Team` model and EF configuration.
- [x] 1.2 Add an EF Core migration that preserves existing teams and updates active team-name uniqueness.
- [x] 2.1 Add captain-owned team delete service behavior.
- [x] 2.2 Block deletion when the team is actively participating in scheduled or in-progress team games/tournaments.
- [x] 2.3 Exclude deleted teams from active team list, lookup, search, public profile, and current-user management projections.
- [x] 2.4 Expose authenticated `DELETE /v{version}/lan/teams/{id}`.
- [x] 3.1 Add tests for captain-only deletion, non-captain rejection, active participation blocking, historical-data preservation, and deleted-team visibility.
- [x] 3.2 Run `dotnet test LAN.API.sln`.
