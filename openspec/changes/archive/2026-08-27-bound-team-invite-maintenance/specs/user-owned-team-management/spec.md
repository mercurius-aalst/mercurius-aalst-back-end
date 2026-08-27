## ADDED Requirements

### Requirement: Bounded invite maintenance
The API MUST keep current-user invite projections fresh without performing global invite maintenance on request threads, and MUST persist invite expiry and retention cleanup through deterministic, configurable, bounded maintenance batches.

#### Scenario: Current-user read does not maintain unrelated invites
- **WHEN** an authenticated user requests their team summary, received invites, or sent invites
- **THEN** the API MUST query only team and invite data relevant to that user or teams they captain
- **AND** MUST NOT expire, delete, scan for events, or publish expiry events for unrelated invites

#### Scenario: Due invite is immediately absent from actionable reads
- **WHEN** a pending invite's expiration timestamp has elapsed but scheduled maintenance has not yet persisted its Expired status
- **THEN** current-user actionable invite projections MUST exclude that invite using its expiration timestamp
- **AND** the read MUST NOT persist an invite status transition

#### Scenario: Expiry maintenance is bounded and deterministic
- **WHEN** one scheduled maintenance cycle finds more due pending invites than the configured batch size
- **THEN** it MUST transition no more than the configured batch size ordered by expiration timestamp and stable invite identifier
- **AND** remaining due invites MUST be left for later cycles

#### Scenario: Retention maintenance is bounded and deterministic
- **WHEN** one scheduled maintenance cycle finds more eligible terminal invites than the configured batch size
- **THEN** it MUST delete no more than the configured batch size ordered by terminal timestamp and stable invite identifier
- **AND** remaining eligible invites MUST be left for later cycles

#### Scenario: Expiry event fan-out is bounded and idempotent
- **WHEN** a maintenance cycle successfully persists pending invites as expired
- **THEN** it MUST attempt one privacy-safe expiry event for each invite transitioned by that cycle
- **AND** MUST NOT publish more expiry events than the configured batch size
- **AND** a later maintenance cycle MUST NOT republish events for those already-expired invites

#### Scenario: Concurrent maintenance instances do not duplicate work
- **WHEN** multiple application instances attempt invite maintenance concurrently
- **THEN** at most one instance MUST own the maintenance batch
- **AND** the other instances MUST leave the owned invites and their expiry events unprocessed

#### Scenario: Maintenance honors cancellation
- **WHEN** application shutdown or caller cancellation is requested during maintenance
- **THEN** database queries, writes, waits, and realtime publication MUST observe the cancellation token
