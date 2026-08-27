## Context

Discovery persists observable search-index rebuild jobs and executes them from one in-process hosted worker. The existing implementation requeues a `Running` job after a fixed 15-minute interval from both admin job creation and worker polling. That can requeue a live long-running job and delays recovery after a process restart.

Repository deployment evidence supports one Discovery worker per database: the API registers one hosted worker and provides no replica or scale configuration. Existing event-dispatch design documentation makes the same single-worker assumption.

## Goals / Non-Goals

**Goals:**

- Recover a job left `Running` by a stopped worker promptly when the replacement worker starts.
- Preserve `Running` state for a legitimate long rebuild while the worker remains alive.
- Preserve the current job state, progress timestamps, source-version merge, coalescing, and bounded failure-error behavior.
- Make requested cancellation recoverable on the next startup rather than recording it as a rebuild failure.

**Non-Goals:**

- Coordinating multiple application instances or atomically claiming work across them.
- Adding leases, heartbeats, locks, raw SQL, concurrency tokens, a configuration section, or schema changes.
- Changing routes, authorization, contracts, public JSON, event contracts, or projection merge behavior.

## Decisions

### Recover only once during worker startup

The hosted worker will call a narrowly named recovery operation before its initial-job check and before its normal claim loop. It will reset persisted `Running` jobs to `Pending` and clear prior start/completion/error state. In the documented single-worker deployment, any persisted `Running` job at startup belongs to an interrupted predecessor, so no elapsed-time heuristic is needed.

Recovery will be removed from the admin creation path and per-cycle worker claim path. Admin requests will continue to coalesce with any pending or running job without mutating it. This avoids turning a long-running active job back to pending.

An elapsed-time threshold was rejected because rebuild duration depends on source size and availability and the repository has no justified operational threshold or options surface. A distributed claim was rejected because no concurrent-instance deployment is documented.

### Preserve cancellation as interruption

`RunNextAsync` will rethrow an `OperationCanceledException` requested through its cancellation token before the ordinary failure handler. The persisted job remains `Running`, making it eligible for the next startup recovery. Genuine failures retain staging cleanup and the existing bounded public error message.

## Risks / Trade-offs

- [A second API instance uses the same database] → Startup recovery or non-atomic claim can overlap active work. Deployment MUST retain one Discovery worker per database; multi-instance ownership requires a separately specified design.
- [A process stays unavailable] → A `Running` job remains observable until the replacement worker starts. This is preferable to falsely reclaiming a live job and is recovered immediately on restart.
- [Cancellation leaves staged rows] → The next execution uses the same job and stages the same unique keys. Startup recovery does not clean staging; cancellation can occur after staging. The worker MUST clear any stale staging for the requeued job before restaging, or recovery MUST clear it as part of the requeue operation.

## Migration Plan

1. Deploy the application change without a database migration.
2. On the first startup, requeue interrupted `Running` jobs before the worker schedules or claims work.
3. Roll back by restoring the prior application version; no schema or data migration is needed.

## Open Questions

None.
