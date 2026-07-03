## Context

Phase 8 separated SignalR realtime publishing from durable module synchronization, but Teams mutations still only emit best-effort realtime notifications after persistence. Later phases need Identity, Teams, Competition, and Discovery to exchange duplicated facts safely without direct implementation coupling.

The current persistence model is still one `MercuriusDBContext`, so Platform eventing infrastructure must be added without extracting DbContexts or introducing module-local persistence ownership early. Module contract assemblies currently reference only `Modules.Shared`; durable payload records must preserve that boundary.

## Goals / Non-Goals

**Goals:**

- Provide a Platform-owned outbox, shared inbox, dispatcher, retry state, and handler resolution path.
- Let module contracts own durable payload records without referencing Platform marker interfaces.
- Persist durable Teams integration events transactionally with Teams lifecycle mutations.
- Support consumer idempotency and version-aware handlers so duplicate and stale events do not overwrite newer projection data.
- Preserve all current public API and SignalR realtime behavior.

**Non-Goals:**

- Extract module DbContexts or move EF ownership out of the current centralized context.
- Replace SignalR realtime notifications with durable integration events.
- Add real Identity, Discovery, or Competition consumers.
- Change routes, authorization, request/response DTOs, OpenAPI shape, or JSON serialization contracts.

## Decisions

- **Use Platform-owned envelopes over Platform marker interfaces.** Durable payload records live in module contract assemblies and are wrapped in Platform envelopes at publish time. This keeps module contracts free of Platform references while still giving Platform a message id, occurred timestamp, type name, and serialized payload.
- **Use one shared inbox table.** `platform.inbox_messages` stores logical consumer name plus message id. Module-local inbox tables are deferred until persistence ownership is tightened in a later phase.
- **Dispatch in-process.** The application remains one deployable unit, so the dispatcher can resolve scoped handlers from DI and process rows from the platform outbox without introducing a broker.
- **Keep event type resolution explicit.** Event payload types are registered with Platform eventing so stored type names are stable and deserialization does not scan arbitrary assemblies.
- **Publish Teams lifecycle facts only.** Team create, rename, delete, member add/remove, and captain transfer produce durable events. Invite status and roster confirmation notifications remain realtime-only in this phase.
- **Use monotonic aggregate versions.** Teams durable events carry the current Team version. Consumers that maintain projections MUST ignore events whose version is older than the stored projection version.

## Risks / Trade-offs

- **Outbox rows can accumulate if the dispatcher is not run** -> Register a reusable dispatcher service and cover it with tests; operational scheduling can be tuned later without changing persistence shape.
- **Handlers might duplicate side effects on retry** -> Record inbox rows per logical consumer and message id in the same scoped processing flow as handler success.
- **Stale events can regress projections** -> Require versioned payloads and cover stale-version rejection with a test projection.
- **Event names can collide with existing realtime records** -> Use durable names such as `TeamCreatedIntegrationEvent` and leave existing realtime records untouched.
- **Centralized DbContext remains a transitional dependency** -> Keep schema additions limited to Platform tables and defer module-local persistence to later phases.

## Migration Plan

Add the Platform tables through an EF migration. The tables are additive, so rollback removes only the platform outbox/inbox tables and Teams version column added for durable event ordering. No data backfill is required because existing Teams rows can start with version `0` and increment on future lifecycle mutations.

## Open Questions

None. Future phases can decide whether to enable hosted polling, module-local inboxes, or real Discovery/Identity consumers.
