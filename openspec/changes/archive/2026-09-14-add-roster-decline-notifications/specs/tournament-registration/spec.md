## ADDED Requirements

### Requirement: Roster selection response actions

An authenticated selected roster member MUST be able to accept or decline their own actionable pending roster selection. A decline MUST release that user from the tournament roster while retaining the pending team registration and every other roster member's recorded response.

#### Scenario: Selected member declines a pending roster place
- **WHEN** an authenticated selected member declines their own pending roster place while the tournament is scheduled
- **THEN** the API MUST remove only that member from the pending team roster
- **AND** the API MUST retain the team registration in pending confirmation state
- **AND** the declining user MUST become eligible for other participation in that tournament subject to the normal eligibility rules

#### Scenario: Decline preserves other responses
- **WHEN** one pending selected member declines
- **THEN** confirmed, auto-confirmed, and pending responses for all other roster members MUST remain unchanged
- **AND** the captain MUST be able to repair the incomplete registration by submitting an exact-size replacement roster

#### Scenario: Decline retry is privacy-safe and idempotent
- **WHEN** an authenticated user declines a roster-member identifier that is missing, no longer pending, or not owned by that user
- **THEN** the API MUST make no state change
- **AND** MUST return the same successful no-content response without disclosing whether another user's selection exists

#### Scenario: Unchanged responses survive roster repair
- **WHEN** a captain replaces an incomplete pending roster with an exact-size roster
- **THEN** the API MUST preserve confirmation status and confirmation time for every unchanged selected member
- **AND** MUST require a response only from newly selected non-captain members

### Requirement: Current-user roster selection notifications

The API MUST expose a bounded authenticated query for the current user's actionable pending roster selections. Pending roster-member rows MUST be the authoritative notification source so the result remains correct after reload without separate notification persistence.

#### Scenario: Current user lists pending roster selections
- **WHEN** an authenticated user requests their roster selection notifications
- **THEN** the API MUST return pending selections for scheduled tournaments only
- **AND** each item MUST include roster member, tournament, and team identifiers; tournament and team display names; the team logo URL when available; and the selection timestamp

#### Scenario: Notification query is paged and deterministic
- **WHEN** the current user requests a valid page
- **THEN** the API MUST return an envelope containing items, total count, page, and page size
- **AND** MUST order selections by newest selection timestamp and then roster-member identifier
- **AND** omitted paging values MUST default to page 1 and page size 20
- **AND** positive page size MUST be capped at 50

#### Scenario: Invalid notification page is rejected
- **WHEN** page or page size is less than one
- **THEN** the API MUST return a validation problem before querying roster selections

#### Scenario: Resolved selection disappears after reload
- **WHEN** a pending selection is confirmed, declined, withdrawn by roster replacement, or removed with its team registration
- **THEN** it MUST no longer appear in the authoritative current-user notification query

### Requirement: Roster response realtime invalidation

The API MUST attempt roster confirmation realtime delivery after the corresponding database commit so connected user and team clients can refresh their authoritative state. Realtime delivery is advisory; the pending-selection query remains authoritative after reload or reconnect.

#### Scenario: Confirmation publishes committed change
- **WHEN** a selected member confirms a pending roster place successfully
- **THEN** the API MUST attempt a `Confirmed` roster confirmation change to the affected user and team targets after commit

#### Scenario: Decline publishes committed change
- **WHEN** a selected member declines a pending roster place successfully
- **THEN** the API MUST attempt a `Declined` roster confirmation change to the affected user and team targets after commit

#### Scenario: Replacement publishes changed actionable selections
- **WHEN** a captain replaces a pending roster
- **THEN** the API MUST attempt `Withdrawn` for removed pending selections and `Pending` for newly selected pending members after commit
- **AND** MUST NOT publish a new pending change for unchanged roster members whose response is preserved

#### Scenario: Advisory delivery fails after a member response

- **WHEN** confirmation or decline is committed and realtime delivery fails
- **THEN** the API MUST still report the committed member response as successful
- **AND** a later pending-selection query MUST reflect the committed state

### Requirement: Exact-size roster activation

A team tournament registration MUST become active only when its current roster has the tournament's exact required size and every current roster member is confirmed or auto-confirmed.

#### Scenario: Incomplete roster cannot activate
- **WHEN** a member confirms after another selected member has declined or been removed
- **THEN** the team registration MUST remain pending until the captain submits an exact-size roster and all required responses are complete
