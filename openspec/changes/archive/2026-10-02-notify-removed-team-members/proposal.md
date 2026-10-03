# Notify users about their membership changes

## Why

When a connected member is removed or leaves a team, the API revokes that member's team-group access before publishing the membership event. Because the event currently targets only the team group, the affected user's other open pages remain stale until they reload.

## What Changes

Keep the existing event names and payloads, and send membership-change events to both the current team group and the affected user's authenticated personal group. When a team is deleted, snapshot its members and active pending invites before deletion, revoke all team-group access after commit, then invalidate each affected user's personal group. This lets affected users refresh their own views while preserving the team-group revocation boundary.
