## 1. Data Model and Migration

- [x] 1.1 Extend team invite state to include cancellation metadata and the Cancelled status.
- [x] 1.2 Extend team invite state to include expiration metadata and the Expired status.
- [x] 1.3 Add optional team logo metadata to the Team model and EF configuration.
- [x] 1.4 Add indexes or constraints for captain lookups, membership/invite projections, duplicate pending invite prevention, and retention cleanup.
- [x] 1.5 Create an EF Core migration that preserves existing teams and backfills captain membership if needed.
- [x] 1.6 Update model snapshot and migration tests for the new columns, enum/state changes, and indexes.

## 2. Service Logic

- [x] 2.1 Add current-user team creation that derives the captain from the authenticated profile and enforces the two-captained-teams limit.
- [x] 2.2 Add captain authorization checks for invite, cancel invite, captain transfer, and logo operations.
- [x] 2.3 Implement invite cancellation and strengthen invite accept/decline so only the recipient can respond.
- [x] 2.4 Implement invite expiration and retention cleanup for expired terminal invites.
- [x] 2.5 Enforce duplicate pending invite blocking and declined-invite resend allowance/cooldown in service and persistence paths.
- [x] 2.6 Implement member leave validation, including captain leave rejection and InProgress, Completed, and Canceled tournament roster blocking.
- [x] 2.7 Implement captain transfer validation, including current-member and recipient captain-limit checks.
- [x] 2.8 Implement current-user team and invite projection queries without N+1 loading.

## 3. Logo Handling

- [x] 3.1 Extend the current file service with supported image type validation, size validation, generated storage key/path safety, delete behavior, and safe image serving assumptions.
- [x] 3.2 Implement team logo upload and replace behavior that stores only safe server-generated references.
- [x] 3.3 Implement team logo removal and previous-logo cleanup or documented best-effort cleanup.
- [x] 3.4 Add safe logo references to private team-management DTOs and the public team profile DTO.

## 4. API Contracts and Endpoints

- [x] 4.1 Add authenticated current-user endpoints for team creation, leave team, current-user summaries, received invites, and sent invites.
- [x] 4.2 Add captain-owned endpoints for inviting users, cancelling invites, transferring captainship, and managing team logos.
- [x] 4.3 Remove admin-only team mutation endpoints that duplicate user-owned team management flows.
- [x] 4.4 Add a SignalR hub or equivalent ASP.NET Core real-time endpoint for team management events.
- [x] 4.5 Publish privacy-safe events after successful invite, membership, and captain transfer commits.
- [x] 4.6 Update DTOs for team summaries, invite summaries, invite responses, captain transfer, logo responses, and real-time event payloads.
- [x] 4.7 Update Swagger/API metadata and authorization attributes so anonymous, authenticated, and captain-only boundaries are explicit.

## 5. Tests

- [x] 5.1 Add tests for authenticated team creation, anonymous rejection, name normalization, duplicate names, and captain limit enforcement.
- [x] 5.2 Add tests for captain-only authorization across invites, cancel invite, captain transfer, and logo management.
- [x] 5.3 Add tests for invite lifecycle states, duplicate pending invite blocking, declined resend allowance/cooldown, invite expiration, retention cleanup, and recipient-only accept/decline.
- [x] 5.4 Add tests for member leave rules across captain leave and InProgress, Completed, and Canceled tournament roster blocking.
- [x] 5.5 Add tests for captain transfer invariants, transfer to non-member rejection, and recipient captain-limit rejection.
- [x] 5.6 Add tests for logo upload, replace, remove, invalid file rejection, path safety, and public logo projection privacy.
- [x] 5.7 Add tests for SignalR event publication and event privacy/scoping for invite, membership, and captain transfer events.
- [x] 5.8 Add tests for current-user projections and verify query shape avoids avoidable N+1 loading.
- [x] 5.9 Add route tests proving removed admin-only team mutation endpoints are no longer exposed.

## 6. Verification

- [x] 6.1 Run `dotnet build LAN.API.sln`.
- [x] 6.2 Run `dotnet test LAN.API.sln`.
- [x] 6.3 Review migration impact, front-end contract impact, Auth0/current-user assumptions, and file-storage configuration changes.
- [x] 6.4 Update this checklist as implementation tasks are completed.
