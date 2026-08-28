## Context

The hosted dispatcher first reads eligible outbox identifiers and then loads each message before invoking its handlers. In a scaled deployment, two processes can observe the same unprocessed row and both run its handlers before either process persists the inbox marker or completion timestamp. The existing delivery contract is at-least-once, but concurrent execution can bypass post-handler inbox deduplication and duplicate non-idempotent side effects.

## Goals / Non-Goals

**Goals:**

- Give at most one dispatcher an active claim on a message at a time.
- Recover automatically when a process stops after claiming but before completion.
- Preserve bounded deterministic selection, retries, dead-lettering, and at-least-once semantics.
- Keep claim operations short and avoid a database transaction around handler execution.

**Non-Goals:**

- Exactly-once delivery across external systems.
- Parallel handler execution within one message.
- Changes to event payloads, public APIs, or module contracts.
- General-purpose distributed locking infrastructure.

## Decisions

### Persist a token and lease expiry on each outbox row

Add nullable claim-token and claim-expiry columns to the Platform-owned outbox table. A fresh GUID identifies the dispatcher attempt; the expiry uses the injected `TimeProvider` so behavior remains deterministic in tests.

This is preferred over holding `FOR UPDATE` locks while handlers run because handler duration and external side effects would otherwise extend database transactions. It is also preferred over process-local locks, which do not coordinate application instances.

### Claim with a conditional atomic update

After deterministic identifier selection, the dispatcher conditionally updates each candidate only when it remains eligible and has no live claim. The update result determines ownership. The message is loaded and dispatched only when exactly one row was updated for the generated token.

This preserves the current bounded selection and per-message isolation while preventing competing workers from both entering handler execution.

### Treat expired claims as eligible

A claim whose expiry is at or before the current time is reclaimable. Successful completion and recorded failure both clear the claim fields. A process stop leaves the claim intact until expiry, after which normal dispatch resumes and existing inbox or handler idempotency protects at-least-once recovery.

### Verify ownership on terminal updates

Completion and failure persistence MUST apply only while the message still carries the dispatcher's claim token. This prevents a delayed worker from overwriting the state written by a later owner after lease expiry.

## Risks / Trade-offs

- **A handler may run longer than the lease and overlap a retry** → Use a conservative lease duration well above expected in-process handler latency and assert ownership before terminal writes; operational latency monitoring remains advisable.
- **A schema migration is required before the new binary runs** → Add only nullable columns so existing rows remain immediately eligible and rollback can drop the columns without data conversion.
- **Contended batches may process fewer than the requested batch size** → Accept this bounded behavior; the next worker loop retries selection without correctness loss.
- **At-least-once delivery still permits replay after process failure** → Preserve inbox and handler-level idempotency; this change targets concurrent ownership, not exactly-once delivery.

## Migration Plan

1. Add nullable claim-token and claim-expiry columns and an index supporting eligible-message selection.
2. Deploy the dispatcher code that conditionally claims rows.
3. Monitor dispatch failures, lease expiry recovery, and outbox backlog.
4. Roll back by reverting the dispatcher and dropping the additive claim columns/index.

## Open Questions

None.
