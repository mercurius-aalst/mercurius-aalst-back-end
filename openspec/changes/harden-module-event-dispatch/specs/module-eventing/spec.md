## MODIFIED Requirements

### Requirement: In-process outbox dispatch
The system SHALL provide an in-process dispatcher that resolves registered handlers and dispatches eligible outbox messages with exclusive database-visible ownership.

#### Scenario: Dispatcher exclusively claims a message
- **WHEN** one or more application instances attempt to dispatch the same eligible outbox message
- **THEN** exactly one dispatcher MUST atomically claim that message before entering any handler
- **AND** all other dispatchers MUST skip the message while that claim remains valid

#### Scenario: Long-running handler retains ownership
- **WHEN** a handler runs longer than the initial lease duration while its dispatcher remains healthy
- **THEN** the dispatcher MUST renew its lease before expiry
- **AND** another dispatcher MUST NOT concurrently enter a handler for that message

#### Scenario: Abandoned lease is recovered
- **WHEN** a claimed message's owner stops renewing its lease and the lease expires
- **THEN** another dispatcher MUST be able to atomically claim and dispatch the message

#### Scenario: Dispatcher invokes handler
- **WHEN** an eligible outbox message has a registered payload type and matching handler
- **THEN** the owning dispatcher MUST deserialize the payload, invoke the handler, and mark the outbox message as processed after successful handling

#### Scenario: Handler failure records retry state
- **WHEN** a handler fails while processing an outbox message before the maximum attempt count
- **THEN** the owning dispatcher MUST leave the message pending, increment its retry count, record last-attempt and truncated error information, and schedule its next attempt

#### Scenario: Only the current owner finalizes a claim
- **WHEN** a dispatcher no longer owns a message after handler execution
- **THEN** it MUST NOT mark that message processed, schedule its retry, or move it to dead-letter state

#### Scenario: Dispatch cancellation releases owned work
- **WHEN** dispatch is cancelled while a message is owned
- **THEN** the dispatcher MUST propagate cancellation and release its claim without counting a failed attempt when it can still prove ownership

## ADDED Requirements

### Requirement: Bounded retry and dead-letter lifecycle
The system SHALL retry failed outbox messages on a deterministic bounded schedule and SHALL stop automatic delivery after the configured maximum attempt count.

#### Scenario: Failure uses capped exponential backoff
- **WHEN** attempt number `n` fails below the maximum attempt count
- **THEN** the next attempt MUST be scheduled after `baseDelay * 2^(n-1)`
- **AND** the scheduled delay MUST NOT exceed the configured maximum retry delay

#### Scenario: Exhausted message becomes dead-lettered
- **WHEN** a failed attempt reaches the configured maximum attempt count
- **THEN** the dispatcher MUST record an explicit dead-letter timestamp and terminal error state
- **AND** automatic dispatch MUST NOT select that message again

#### Scenario: Poison messages do not starve later work
- **WHEN** enough older poison messages fail to fill or cross a dispatch batch boundary
- **THEN** their delayed or terminal states MUST be excluded from subsequent eligible claims
- **AND** later healthy eligible messages MUST continue to be dispatched

### Requirement: Bounded terminal outbox retention
The system SHALL periodically remove old successful and dead-lettered outbox records in bounded batches while preserving shared inbox idempotency for every pending or retained outbox record.

#### Scenario: Successful records expire after success retention
- **WHEN** a processed outbox record is older than the configured success retention period
- **THEN** cleanup MUST make it eligible for deletion

#### Scenario: Dead-lettered records expire after dead-letter retention
- **WHEN** a dead-lettered outbox record is older than the configured dead-letter retention period
- **THEN** cleanup MUST make it eligible for deletion

#### Scenario: Cleanup is bounded and preserves recent records
- **WHEN** terminal cleanup runs with more expired records than the configured cleanup batch size
- **THEN** it MUST delete no more than that batch size in deterministic terminal-time order
- **AND** it MUST preserve pending, actively leased, and terminal records that have not reached their retention cutoff
- **AND** it MUST delete inbox markers only for the terminal outbox message ids selected in that cleanup batch and in the same transaction

### Requirement: At-least-once module event delivery
The system SHALL describe durable in-process module event dispatch as at-least-once delivery and MUST rely on inbox idempotency or handler-level idempotency for repeated attempts.

#### Scenario: Lease recovery can repeat an attempt
- **WHEN** ownership is lost after a handler side effect but before durable completion is recorded
- **THEN** a later owner MAY attempt the message again
- **AND** the system MUST NOT represent the delivery as exactly once
