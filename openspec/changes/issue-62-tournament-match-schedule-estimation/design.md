## Context

Games currently move from scheduled to in-progress with `Game.Start()`, which sets `StartTime` to `DateTime.UtcNow`. Matches also have `StartTime` and `EndTime`, but match moderators only generate bracket structure. The front-end needs planned schedule fields before and during a tournament without conflating estimates with actual lifecycle timestamps.

## Goals / Non-Goals

**Goals:**
- Persist schedule configuration on games while they are still scheduled.
- Generate deterministic estimated match windows when a tournament is started and matches are created.
- Expose schedule configuration and estimates through the existing game and match response DTOs.
- Keep existing create, update, start, reset, cancel, complete, and delete behavior intact.

**Non-Goals:**
- Replacing actual started/completed timestamps with planned estimates.
- Supporting live rescheduling after match generation has started.
- Reworking bracket generation algorithms beyond adding schedule assignment.

## Decisions

- Treat planned schedule fields separately from actual lifecycle fields. Use explicit names such as planned/estimated values during implementation so API consumers can distinguish schedule estimates from actual completion state.
- Keep schedule input on game create/update DTOs because admins configure these values before a tournament starts.
- Calculate match duration from `GameFormat`: Best of 1 equals one configured single-game duration, Best of 3 equals three, and Best of 5 equals five. Finals use `FinalsFormat`.
- Assign schedule windows after the match moderator returns all matches. Group matches by round metadata, schedule matches in the same round at the same start time, and move to the next round after the longest match duration plus the configured break.
- Compute the estimated tournament end from the last generated match end time and return it on the game response.
- Validate positive durations and safe date ranges in the service/domain layer, with DTO annotations only as the first line of defense.

## Risks / Trade-offs

- Existing `StartTime` and `EndTime` names may tempt implementation to reuse actual timestamps for estimates. The implementation should avoid ambiguous naming.
- Double-elimination lower bracket rounds may not map perfectly to real-world parallel scheduling. A deterministic first pass grouped by generated round number is acceptable and can be refined later.
- EF in-memory tests can miss relational migration issues, so migration/backfill assertions should be reviewed carefully when persistence fields are added.
