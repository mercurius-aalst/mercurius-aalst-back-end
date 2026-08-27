## MODIFIED Requirements

### Requirement: In-process outbox dispatch
The system SHALL provide one hosted in-process dispatcher that resolves registered handlers and processes a deterministic bounded set of eligible outbox messages.

#### Scenario: Dispatcher isolates message tracking
- **WHEN** the dispatcher selects a batch of eligible messages
- **THEN** it MUST select their identifiers without tracking in occurrence-time and identifier order
- **AND** it MUST load and process each selected message independently

#### Scenario: Earlier failure does not detach later work
- **WHEN** an earlier selected message fails and failure recovery clears tracked state
- **THEN** the dispatcher MUST still load every later selected identifier independently
- **AND** a later successful message MUST persist its processed timestamp

#### Scenario: Dispatcher invokes handlers
- **WHEN** an eligible outbox message has a registered payload type and matching handlers
- **THEN** the dispatcher MUST deserialize the payload and invoke handlers in their registered order
- **AND** it MUST mark the outbox message processed only after all handlers complete successfully

#### Scenario: Dispatch cancellation propagates
- **WHEN** dispatch is cancelled
- **THEN** the dispatcher MUST propagate cancellation without recording it as a failed delivery attempt

## ADDED Requirements

### Requirement: Bounded retry and dead-letter lifecycle
The system SHALL retry failed outbox messages on a deterministic capped schedule and SHALL stop automatic delivery after five failed attempts.

#### Scenario: Failure records a deferred retry
- **WHEN** a handler fails before the fifth failed attempt
- **THEN** the dispatcher MUST increment the retry count and record the attempt time and truncated error
- **AND** it MUST schedule the next attempt after a deterministic delay that does not exceed the internal cap
- **AND** an immediate subsequent dispatch MUST NOT select that message

#### Scenario: Exhausted message becomes dead-lettered
- **WHEN** the fifth delivery attempt fails
- **THEN** the dispatcher MUST record a dead-letter timestamp and clear its next-attempt timestamp
- **AND** automatic dispatch MUST NOT select that message again

#### Scenario: Poison messages do not starve later work
- **WHEN** older poison messages fill or cross a bounded dispatch batch
- **THEN** their deferred or dead-lettered state MUST exclude them from later eligible selections
- **AND** later healthy eligible messages MUST continue to be dispatched

### Requirement: At-least-once module event delivery
The system SHALL describe durable in-process module event dispatch as at-least-once delivery and MUST rely on inbox idempotency or handler-level idempotency for repeated attempts.

#### Scenario: Completion persistence can follow handler effects
- **WHEN** a process stops after a handler effect but before durable message completion is recorded
- **THEN** a later dispatch MAY attempt the message again
- **AND** the system MUST NOT represent delivery as exactly once

#### Scenario: Terminal records remain available
- **WHEN** an outbox message succeeds or becomes dead-lettered
- **THEN** the system MUST retain its outbox row and associated inbox markers unless a separate manual operation removes them
