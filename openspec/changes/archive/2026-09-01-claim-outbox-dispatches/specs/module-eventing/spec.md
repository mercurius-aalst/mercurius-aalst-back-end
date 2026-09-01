## ADDED Requirements

### Requirement: Exclusive recoverable outbox claims
The system SHALL atomically claim an eligible outbox message before invoking any handler and MUST allow no more than one active claim for the same message.

#### Scenario: Overlapping dispatchers compete for one message
- **WHEN** two dispatchers attempt to claim the same eligible outbox message concurrently
- **THEN** exactly one dispatcher MUST acquire the active claim
- **AND** only the claim owner MUST invoke handlers for that dispatch attempt

#### Scenario: Interrupted claim expires
- **WHEN** a dispatcher stops after claiming a message without recording completion or failure
- **THEN** the message MUST remain unavailable until the claim lease expires
- **AND** the message MUST become eligible for a later at-least-once dispatch after expiry

#### Scenario: Dispatch releases its claim
- **WHEN** the claim owner records successful completion or a failed delivery attempt
- **THEN** the system MUST clear the claim token and lease expiry as part of that state update

#### Scenario: Expired owner cannot overwrite later state
- **WHEN** a dispatcher's lease expires and another dispatcher claims the same message
- **THEN** the expired dispatcher MUST NOT persist completion or failure state for the later owner's claim
