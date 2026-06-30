## ADDED Requirements

### Requirement: Durable module event persistence
The system SHALL persist module integration events to a Platform-owned outbox before dispatching them to consumers.

#### Scenario: Event is saved to outbox
- **WHEN** a module publishes a durable integration event as part of a supported business mutation
- **THEN** the system MUST save an outbox message with a message id, registered event type, serialized payload, occurrence timestamp, retry count, and processing state

#### Scenario: Public API behavior is unchanged
- **WHEN** durable module eventing is introduced
- **THEN** existing public routes, authorization rules, request DTOs, response DTOs, JSON shapes, SignalR group names, SignalR client method names, and realtime payload timing MUST remain unchanged

### Requirement: Transactional event publication
The system SHALL save supported business mutations and their durable outbox messages in the same database commit.

#### Scenario: Business change and event commit together
- **WHEN** a supported Teams lifecycle mutation succeeds
- **THEN** the Teams state change and its durable outbox message MUST both be committed

#### Scenario: Event enqueue failure prevents commit
- **WHEN** a supported Teams lifecycle mutation cannot enqueue its durable integration event
- **THEN** the Teams state change MUST NOT be committed

### Requirement: In-process outbox dispatch
The system SHALL provide an in-process dispatcher that resolves registered handlers and dispatches pending outbox messages.

#### Scenario: Dispatcher invokes handler
- **WHEN** a pending outbox message has a registered payload type and matching handler
- **THEN** the dispatcher MUST deserialize the payload, invoke the handler, and mark the outbox message as processed after successful handling

#### Scenario: Handler failure records retry state
- **WHEN** a handler fails while processing an outbox message
- **THEN** the dispatcher MUST leave the message pending and update retry count and last error information

### Requirement: Inbox idempotency
The system SHALL suppress duplicate consumer handling with a shared Platform inbox keyed by logical consumer name and message id.

#### Scenario: Duplicate event is ignored
- **WHEN** a consumer has already processed a message id
- **THEN** the dispatcher MUST skip invoking that consumer for the duplicate message

#### Scenario: Retry does not duplicate side effects
- **WHEN** an outbox message is retried after at least one consumer has already processed it
- **THEN** previously completed consumers MUST NOT run again for the same message id

### Requirement: Versioned integration events
The system SHALL use versioned durable event payloads for Teams lifecycle facts.

#### Scenario: Teams publishes versioned lifecycle events
- **WHEN** Teams creates, renames, deletes, adds a member, removes a member, or transfers captain ownership
- **THEN** the corresponding durable integration event payload MUST include the Team id and current monotonic Team version

#### Scenario: Stale version does not overwrite newer projection
- **WHEN** a consumer receives a Teams integration event whose version is older than the stored projection version
- **THEN** the consumer MUST ignore the stale event and keep the newer projection data
