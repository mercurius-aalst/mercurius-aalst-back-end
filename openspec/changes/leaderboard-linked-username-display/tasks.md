## Backend implementation

- [x] Snapshot the public username when a new linked participant is created and store it in the existing display name.
- [x] Fall back to the generic "Incomplete profile" placeholder when no public username is available, never a real profile name; preserve guest-entered names.
- [x] Resolve the username before persistence so a failed lookup leaves no participant or attempt committed.
- [x] Leave public leaderboard, administrator participant, and finalized placement projections reading the stored display name.
- [x] Add regression tests for the create-time snapshot, the guest name, finalized placements, and the failed-lookup transactional guarantee.
- [x] Run strict OpenSpec validation.
- [ ] Run focused leaderboard tests, synchronize the live leaderboard spec, and archive this change after review.
