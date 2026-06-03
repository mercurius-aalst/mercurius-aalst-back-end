## Why

The redesigned front-end needs tournament and match timing from the API instead of mock data. The current domain has `StartTime` and `EndTime` fields, but they are used as actual lifecycle timestamps and generated matches are not given deterministic estimated schedule windows.

## What Changes

- Add admin-managed tournament scheduling configuration for planned start time, average single-game duration, and round break duration.
- Derive estimated match start/end times from tournament start, bracket rounds, match format, finals format, and configured breaks when matches are generated.
- Return tournament and match schedule fields in the game and match DTOs consumed by public and admin screens.
- Validate schedule fields during create/update and prevent unsafe edits after match generation starts.
- Add regression tests for validation, match schedule generation, finals duration, breaks, and existing lifecycle actions.

## Capabilities

### New Capabilities
- `tournament-schedule-estimation`: Admin-configured tournament schedule inputs and deterministic estimated match timing in API responses.

### Modified Capabilities
- None.

## Impact

- Models and EF migrations for `Game` and possibly `Match`.
- `CreateGameDTO`, `UpdateGameDTO`, `GetGameDTO`, and `GetMatchDTO`.
- `GameService`, match moderator output, and match generation flow.
- `GameEndpoints`, API docs, and `tests/MercuriusAPI.Tests`.
