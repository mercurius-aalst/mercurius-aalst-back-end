## MODIFIED Requirements

### Requirement: Admin search-index rebuild jobs
The API MUST expose `POST /internal/discovery/search-index-rebuild-jobs` and `GET /internal/discovery/search-index-rebuild-jobs/{jobId}` as admin-only internal endpoints. Discovery MUST persist rebuild job status and MUST coalesce a new request with an already pending or running rebuild. Across all API replicas, at most one worker may recover, claim, stage, merge, change rebuild status, or clean staging at a time. The worker MUST hold a PostgreSQL session-level advisory lock on the same pinned physical connection used for all Discovery rebuild operations. Each successful lock acquisition MUST recover interrupted jobs before claiming work. Loss of the owner session MUST abort the ownership cycle; the worker MUST NOT reconnect and continue or run ordinary failure cleanup on a replacement session. Staging MUST remain logged and may be truncated only by the current owner. A failed job on a healthy owner session MUST retain observable failed status and cleanup behavior.

#### Scenario: Admin requests a rebuild
- **WHEN** an admin posts a search-index rebuild request
- **THEN** the API returns an observable pending or running rebuild job identifier

#### Scenario: Rebuild reconstructs current documents
- **WHEN** Discovery processes a pending rebuild job
- **THEN** it obtains privacy-safe source snapshots through module contracts and upserts current documents without requiring public search requests to query source tables

#### Scenario: Non-admin access is rejected
- **WHEN** a caller without the admin role requests or reads a rebuild job
- **THEN** the API rejects the request according to the existing authorization policy

#### Scenario: Failed rebuild is observable
- **WHEN** a rebuild cannot complete for a reason other than requested worker cancellation while its owner session remains healthy
- **THEN** Discovery persists a failed terminal status and a bounded diagnostic message for the job

#### Scenario: Deleted documents do not suppress initial backfill
- **WHEN** Discovery contains only deleted search documents and no completed rebuild exists
- **THEN** the hosted worker schedules an initial rebuild

#### Scenario: A running job is not reclaimed by an admin request
- **WHEN** a rebuild remains running while a worker owns the advisory lock and an admin requests another rebuild
- **THEN** Discovery returns the existing running job without changing its status or progress timestamps

#### Scenario: A second replica cannot recover or mutate an owned rebuild
- **WHEN** one worker owns the rebuild advisory lock and is staging a job
- **THEN** another worker MUST perform no recovery, claim, staging, merge, status update, or cleanup for that job

#### Scenario: Every new owner recovers an interrupted job first
- **WHEN** an owner acquires the lock after a previous owner's session ended during a rebuild
- **THEN** it MUST recover the orphaned running job and stale staged rows before starting further rebuild work
- **AND** recovery MUST occur on acquisition without requiring a worker restart

#### Scenario: Requested cancellation remains recoverable
- **WHEN** the hosted worker cancellation token interrupts a rebuild while the owner session is still usable
- **THEN** Discovery leaves the job running for recovery by the next owner instead of persisting a failed status

#### Scenario: Owner-session loss fences further writes
- **WHEN** the pinned owner connection is lost during staging or merge
- **THEN** the current cycle MUST stop without reconnecting for more Discovery writes or failure cleanup
- **AND** any open merge transaction MUST roll back with the lost session
- **AND** a later owner MUST recover and safely rebuild

#### Scenario: Rebuild lock and mutations share one PostgreSQL session
- **WHEN** a worker holds rebuild ownership
- **THEN** lock acquisition, recovery, staging, merge, status writes, and cleanup MUST use the same physically open PostgreSQL session
- **AND** ownership MUST be explicitly released on that session before it is returned to a connection pool

#### Scenario: Staging cleanup is owner-protected
- **WHEN** a rebuild successfully merges, fails on a healthy owner session, or recovers an interrupted job
- **THEN** staging cleanup MUST occur only while the worker owns the lock
- **AND** PostgreSQL cleanup MAY use `TRUNCATE` while ownership is held
- **AND** the staging table MUST remain logged

## ADDED Requirements

### Requirement: Safe deployment of rebuild ownership
The deployment and rollback runbook MUST prevent any overlap between old lock-unaware rebuild workers and new lock-aware workers during the initial rollout or rollback. Once all running workers honor the advisory lock, rolling overlap between new-version replicas MUST be safe.

#### Scenario: Initial rollout hands off rebuild workers
- **WHEN** the first lock-aware version is deployed
- **THEN** the deployment gate MUST stop or otherwise exclude all old rebuild workers before any new rebuild worker starts

#### Scenario: Rollback prevents mixed-version rebuild ownership
- **WHEN** the lock-aware version is rolled back
- **THEN** the rollback gate MUST prevent old lock-unaware workers from overlapping with any remaining lock-aware worker

#### Scenario: New-version replicas overlap safely
- **WHEN** multiple lock-aware replicas run during a rolling deployment
- **THEN** only the advisory-lock owner may recover or mutate a rebuild

### Requirement: Search projection storage is tuned for rebuild updates
The `discovery.search_documents` table MUST use `fillfactor = 90` and `autovacuum_vacuum_scale_factor = 0.05`. The rebuild staging table MUST remain logged. A schema migration MUST apply these settings and MUST restore the prior defaults and search indexes exactly on downgrade.

#### Scenario: Projection update settings are applied
- **WHEN** the issue-140 migration is applied
- **THEN** the projection table MUST have the specified fillfactor and autovacuum scale factor
- **AND** the migration MUST preserve all existing document data

#### Scenario: Downgrade restores the prior index definitions
- **WHEN** the issue-140 migration is rolled back
- **THEN** the prior schema and search indexes MUST be restored, including their historical `game` predicates
