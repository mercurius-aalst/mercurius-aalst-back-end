## 1. Model and API Contract

- [x] 1.1 Add explicit schedule configuration and estimated end fields to the game model and EF mapping.
- [x] 1.2 Add any needed estimated schedule fields to match persistence without confusing them with actual lifecycle timestamps.
- [x] 1.3 Create an EF migration and update the model snapshot.
- [x] 1.4 Extend create/update/get game DTOs and get match DTOs with schedule fields.

## 2. Scheduling Behavior

- [x] 2.1 Validate planned start time, average single-game duration, and round break duration on create/update.
- [x] 2.2 Prevent schedule configuration changes after matches have been generated or the tournament is no longer scheduled.
- [x] 2.3 Add a schedule assignment step after match moderator generation.
- [x] 2.4 Compute estimated match durations from regular format and finals format.
- [x] 2.5 Compute and persist the estimated tournament end time.

## 3. Regression Coverage

- [x] 3.1 Add tests for create/update schedule validation and unauthorized schedule edits.
- [x] 3.2 Add tests for generated match start/end times, round breaks, and finals format duration.
- [x] 3.3 Add tests proving existing tournament actions still work.
- [x] 3.4 Run `dotnet test` for the solution.
