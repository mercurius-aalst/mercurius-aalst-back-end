# notify-removed-team-members

- [x] Publish membership-change events to the team group and the affected user's personal group.
- [x] Invalidate former members' personal groups after a committed team deletion and team-group revocation.
- [x] Invalidate pending invitees' personal groups after their invite is removed by team deletion.
- [x] Add a regression test for the event targets and retain the revoke-before-broadcast ordering test.
- [x] Verify connected members and invitees refresh after team deletion without restoring team-group access.
- [x] Run focused team tests and strictly validate this change.
