## Context

Media writes happen outside the relational database transaction. Game and Team logo mutations currently save a file before `SaveChangesAsync`; Sponsorship does so before `SponsorshipOutboxWriter` commits its state and durable business event. Replacements and deletions either never retire the previous asset or call Media without protecting post-commit failures. Team soft deletion is additionally wrapped by `TeamEventPublishingDecorator`, whose outer transaction commits the Team state and durable event before SignalR access is revoked.

Media already generates unique `images/<generated>.webp` references and restricts deletion to safe generated or supported legacy keys under the configured root. Tournament registrations intentionally retain `TeamLogoUrlAtRegistration`, including after a Team is soft-deleted.

## Goals / Non-Goals

**Goals:**

- Compensate a newly saved image if its owning mutation fails or is cancelled before the actual persistence boundary confirms commit.
- Retire replaced or deleted owned assets only after the corresponding state and durable business event commit.
- Preserve committed API success when physical post-commit deletion fails, while logging the orphan.
- Preserve every current Team-logo reference and every historical tournament-registration snapshot with bounded queries.
- Preserve current SignalR revocation order, durable business events, routes, DTOs, JSON, schema, and configuration.

**Non-Goals:**

- Durable cleanup messages, ownership/reference tables, a reaper, dead-letter replay, or an operations UI.
- Reclaiming a protected Team logo after its final historical registration is later removed.
- Making filesystem and database changes atomically crash-safe.
- Supporting manual cross-domain reuse of Media-generated storage keys as an ownership model.

## Decisions

- **Use synchronous idempotent cleanup.** Each owning service calls `IMediaModule.DeleteImageAsync` with `CancellationToken.None`. Adding an outbox cleanup event would expand the durable integration surface and still require manual handling after the dispatcher's fifth-attempt dead letter. For the current local filesystem and low-volume administrative mutations, logged best-effort deletion is proportionate.

- **Track commit at the real persistence boundary.** A newly returned asset is compensatable until `GameService.SaveChangesAsync`, `TeamService.SaveChangesAsync`, or `SponsorshipOutboxWriter.SaveAndPublishAsync` returns successfully. A commit-success flag is set immediately at that boundary; later DTO mapping or readback failures do not delete an asset already referenced by committed state.

- **Preserve the original pre-commit failure.** Compensation catches and logs its own deletion exception before rethrowing the original mutation, cancellation, database, or outbox failure. It skips a blank reference and, for replacements, skips a new reference ordinally equal to the previous current reference.

- **Make post-commit retirement non-fatal.** Replacement and deletion save first, then attempt physical deletion with a non-cancelled token. Deletion exceptions are logged and swallowed so an already committed mutation is not presented as rolled back.

- **Centralize Team reference decisions in the existing service.** `TeamService` checks active Team rows for the candidate URL and asks the existing `ITeamCompetitionReadService` one new question: whether any tournament registration has an ordinally equal `TeamLogoUrlAtRegistration`. Query failure logs and retains the file. Replacement/removal call this logic after their direct save. The decorator captures the prior logo inside its transaction, preserves the existing post-commit SignalR revocation, then asks `TeamService` to retire the logo.

- **Retain Media's storage-key safety boundary.** Business modules do not parse paths. Blank and unchanged references are skipped before invoking Media; external, default/static, traversal, non-image, and arbitrary references remain no-ops in `FileSystemMediaModule`.

## Risks / Trade-offs

- **[The process terminates between commit and deletion, or during compensation]** → The file can remain orphaned; operators must remove it manually. No durable retry is introduced.
- **[Physical deletion repeatedly fails]** → The committed database state remains authoritative and a warning identifies the residual orphan.
- **[A Team-logo reference query fails]** → Cleanup fails safe by retaining the file and logging the query failure.
- **[A registration concurrently snapshots the old Team logo after the bounded reference check]** → There is a narrow cross-request race because no shared ownership table or serializable cross-module lock is introduced. Media-generated keys are immutable and unique, and the post-commit query minimizes the window.
- **[Storage is local to multiple application nodes rather than shared]** → Cleanup affects only the addressed storage volume; deployment must provide shared or otherwise coordinated storage.

## Migration Plan

1. Deploy the service and contract changes together; no schema or configuration migration is required.
2. Existing files and references remain valid. Cleanup begins only on future mutations.
3. Rollback restores the former lifecycle behavior without database changes; files already retired are not restored.

## Open Questions

None. Durable reclamation and cross-domain ownership tracking require separate operational evidence and specification.
