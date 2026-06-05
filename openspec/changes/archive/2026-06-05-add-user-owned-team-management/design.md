## Context

`Team` already tracks a captain, members, and invites, and `TeamService` already centralizes team-name normalization and invite responses. Current route mapping is still admin-oriented, with team creation and membership mutation driven by supplied user ids rather than the authenticated current user. `TeamInviteStatus` supports pending, accepted, and declined states, but issue #82 also requires cancellation, expiration, captain-owned invite views, leave rules tied to tournament roster state, real-time front-end updates, and logo upload management.

The implementation must preserve the public team profile privacy contract while adding authenticated self-service endpoints for the redesigned front end. Existing admin-only team management endpoints should be retired rather than carried forward. Existing file services should be extended for team logo conversion, validation, deletion, and storage safety.

## Goals / Non-Goals

**Goals:**

- Add authenticated current-user team management without weakening existing admin oversight.
- Keep captain/member/invite authorization checks in server-side service logic, not only endpoint filters.
- Persist team membership and invite state so listings can be queried efficiently without N+1 loading.
- Reuse existing team-name normalization and file-service patterns where they fit.
- Make logo upload serving safe by only accepting supported image inputs and returning controlled relative URLs or keys.
- Publish team management changes through SignalR so Blazor clients can react to invite, membership, and captain changes without polling.
- Define invite expiration and retention so stale invites can be cleaned up while preserving data needed for cooldown and audit.

**Non-Goals:**

- Replacing the public team profile or search contracts beyond adding an optional safe logo reference.
- Allowing multiple captains, co-captains, or team roles beyond the single captain plus members model.
- Reworking tournament registration semantics except where required to block unsafe member leave operations.

## Decisions

### Authenticated user ownership endpoints

Add authenticated endpoints that derive the acting user from the current principal/profile instead of accepting a mutable `CaptainUserId` or target user id for self-service operations. Remove admin-only team management endpoints from the public API surface and route team mutations through current-user service methods that verify the acting user is the current captain, invite recipient, or member as appropriate.

Alternative considered: relax existing `/lan/teams` admin routes. That would risk preserving user-id spoofing surfaces and make admin-vs-user behavior harder to test.

### Single captain with membership invariant

Keep `Team.CaptainUserId` as the single captain source of truth and require the captain to also be in `Team.Members`. Captain transfer will load the team with members, verify the acting user is the current captain, verify the new captain is a current member, update only `CaptainUserId`, and save in one transaction.

Alternative considered: introduce a membership role enum. That is more flexible, but the issue requires exactly one captain and current data already models that directly.

### Invite lifecycle and anti-spam

Extend `TeamInviteStatus` with `Cancelled` and `Expired`, and track response, cancellation, and expiration timestamps. Enforce one pending invite per team/user with a database-backed uniqueness strategy where possible, and allow a configured number of declined resend attempts before applying the resend cooldown. Retain declined/expired terminal invites only while needed for cooldown or audit. Captains cancel only pending invites for teams they captain; recipients accept or decline only their own pending invites. Expired invites are not actionable and should be excluded from pending invite projections.

Alternative considered: keep every invite forever. That is unnecessary for the product and increases noise, so retention should be bounded by configured cooldown/audit windows with a cleanup path.

### Leave rules use tournament state checks

A member may leave a team only if the team is not in a protected team tournament roster. `InProgress`, `Completed`, and `Canceled` tournament statuses are leave-blocking for registered teams. The service should query registered team games and match/tournament status in a targeted projection rather than loading all tournaments and matches into memory.

Alternative considered: allow leave and repair tournament rosters later. That creates inconsistent tournament state and conflicts with the issue's security requirements.

### Logo storage metadata and safe serving

Store optional team logo metadata on `Team`, such as a relative URL/key and enough information to replace/remove the previous logo. Extend the current file service with team-logo validation, generated server-side names, replacement, and delete/remove semantics. Uploads should pass through image validation and conversion, reject unsafe file types/sizes, avoid user-controlled paths, and be served as inert image content. Removing a logo clears team metadata and deletes the stored file through the file service.

Alternative considered: store arbitrary user-supplied URLs. That would be simpler, but it would bypass upload validation and safe content serving.

### Real-time team events

Use ASP.NET Core SignalR for team management events because the front end is Blazor and SignalR is already the platform-native real-time option. Publish events after successful commits for invite created/cancelled/accepted/declined/expired, member left, and captain transferred. Logo upload, replace, and remove operations update REST projections only and do not publish real-time notifications. Event payloads should be privacy-safe and scoped so recipients only receive events for teams or invites they are allowed to know about.

Alternative considered: client polling. Polling is simpler but creates stale invite/member UI and unnecessary repeated list requests.

### Current-user projections

Add projection DTOs for teams captained by the current user, teams where the current user is a member, pending received invites, and pending sent invites for teams captained by the current user. Queries should use `AsNoTracking`, filtered includes or direct projections, and stable ordering.

Alternative considered: reuse admin `GetTeamDTO` everywhere. Admin DTOs expose internal ids and do not naturally express invite summary state for the front end.

## Risks / Trade-offs

- Captain limit race conditions -> enforce in service and consider a transaction or database constraint strategy so concurrent team creation cannot exceed two captainships.
- Invite uniqueness race conditions -> add supporting indexes/constraints where EF/PostgreSQL can represent the pending-only rule, and keep service validation for clear API errors.
- File cleanup on replace/remove may fail -> extend the file service with explicit delete behavior and log cleanup failures so orphan cleanup can be retried or handled operationally.
- Public logo URLs may reveal storage details -> return controlled relative paths/keys only and continue omitting private member/invite fields.
- SignalR event delivery is best-effort -> keep REST projections authoritative so clients can resync after reconnecting.

## Migration Plan

1. Add model changes for invite cancellation, expiration, optional logo metadata, and any required indexes/constraints.
2. Backfill existing teams so captains are members where any legacy data violates the invariant.
3. Remove admin-only team management endpoints when the authenticated user-owned replacements are available.
4. Apply migration before enabling front-end flows that depend on cancelled/expired invites, SignalR events, or logo metadata.
5. Configure invite retention cleanup and file-storage delete behavior for the deployment environment.
6. Rollback by disabling new self-service endpoints first, then reverting the migration if no new invite/logo data must be retained.

## Open Questions

- None at proposal time.
