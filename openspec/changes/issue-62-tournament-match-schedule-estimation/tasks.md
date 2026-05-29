## 1. Model and API Contract

- [ ] 1.1 Add explicit schedule configuration and estimated end fields to the game model and EF mapping.
- [ ] 1.2 Add any needed estimated schedule fields to match persistence without confusing them with actual lifecycle timestamps.
- [ ] 1.3 Create an EF migration and update the model snapshot.
- [ ] 1.4 Extend create/update/get game DTOs and get match DTOs with schedule fields.

## 2. Scheduling Behavior

- [ ] 2.1 Validate planned start time, average single-game duration, and round break duration on create/update.
- [ ] 2.2 Prevent schedule configuration changes after matches have been generated or the tournament is no longer scheduled.
- [ ] 2.3 Add a schedule assignment step after match moderator generation.
- [ ] 2.4 Compute estimated match durations from regular format and finals format.
- [ ] 2.5 Compute and persist the estimated tournament end time.

## 3. Regression Coverage

- [ ] 3.1 Add tests for create/update schedule validation and unauthorized schedule edits.
- [ ] 3.2 Add tests for generated match start/end times, round breaks, and finals format duration.
- [ ] 3.3 Add tests proving existing tournament actions still work.
- [ ] 3.4 Run `dotnet test` for the solution.
