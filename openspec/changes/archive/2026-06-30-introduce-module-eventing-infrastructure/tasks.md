## 1. OpenSpec And Boundary Decisions

- [x] 1.1 Create proposal, design, and module-eventing spec artifacts documenting the shared inbox, Platform envelope, module payload records, unchanged public API behavior, and realtime/durable separation.

## 2. Platform Contracts And Persistence

- [x] 2.1 Add Platform eventing contracts, envelope types, type registry, publisher, dispatcher interface, and DI registration extensions.
- [x] 2.2 Add Platform outbox/inbox persistence entities and map them through the current `MercuriusDBContext`.
- [x] 2.3 Add an EF migration for `platform.outbox_messages`, shared `platform.inbox_messages`, and Teams version persistence.

## 3. Dispatcher, Retry, And Inbox Idempotency

- [x] 3.1 Implement pending outbox dispatch with event type resolution, scoped handler invocation, processed timestamps, retry count, and last-error updates.
- [x] 3.2 Implement shared inbox duplicate suppression by logical consumer name and message id.

## 4. Teams Versioned Integration Contracts

- [x] 4.1 Add durable Teams integration event payload records with distinct `IntegrationEvent` names and version fields.
- [x] 4.2 Add deterministic persisted Team version increments for create, rename, delete, member add/remove, and captain transfer facts.

## 5. Transactional Teams Publication

- [x] 5.1 Register eventing services and Teams durable event payload types without changing module contract dependency rules.
- [x] 5.2 Enqueue Teams lifecycle integration events transactionally with the corresponding Teams mutation while preserving existing realtime notifications.

## 6. Reliability And Validation

- [x] 6.1 Add targeted tests for eventing registration/model shape, outbox persistence, dispatcher success/failure, inbox idempotency, and stale-version protection.
- [x] 6.2 Add targeted Teams tests for version increments and durable outbox enqueue.
- [x] 6.3 Run required validation and document any environment limitations.
