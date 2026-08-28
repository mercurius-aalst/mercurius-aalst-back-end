## MODIFIED Requirements

### Requirement: Admin search-index rebuild jobs
The API MUST expose `POST /internal/discovery/search-index-rebuild-jobs` and `GET /internal/discovery/search-index-rebuild-jobs/{jobId}` as admin-only internal endpoints. Discovery MUST persist the status of each rebuild job and MUST coalesce a new request with an already pending or running rebuild. Discovery MUST run one hosted rebuild worker per database; multi-instance claim coordination is outside this capability.

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
- **WHEN** a rebuild cannot complete for a reason other than requested worker cancellation
- **THEN** Discovery persists a failed terminal status and a bounded diagnostic message for the job

#### Scenario: Deleted documents do not suppress initial backfill
- **WHEN** Discovery contains only deleted search documents and no completed rebuild exists
- **THEN** the hosted worker schedules an initial rebuild

#### Scenario: An interrupted running job is recovered at startup
- **WHEN** the single Discovery rebuild worker starts and finds a persisted running rebuild job
- **THEN** Discovery requeues the job before initial scheduling or normal worker claims

#### Scenario: A long-running job is not reclaimed by an admin request
- **WHEN** a rebuild remains running while the single Discovery worker is active and an admin requests another rebuild
- **THEN** Discovery returns the existing running job without changing its status or progress timestamps

#### Scenario: Requested cancellation remains recoverable
- **WHEN** the hosted worker cancellation token interrupts a running rebuild
- **THEN** Discovery leaves the job running for recovery by the next worker startup instead of persisting a failed status
