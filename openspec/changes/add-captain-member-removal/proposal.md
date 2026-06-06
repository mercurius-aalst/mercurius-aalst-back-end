## Why

Team captains can invite members and transfer captainship, but they cannot remove a member from a team through the authenticated team-management flow.

## What Changes

- Add a captain-only team member removal endpoint.
- Require authenticated captain authorization before removing a member.
- Block removal only while the team is registered in an in-progress team tournament.
- Publish the existing privacy-safe membership event after successful removal.

## Impact

- Adds one authenticated team route.
- Extends the user-owned team management API contract.
- No database migration required.
