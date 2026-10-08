## Search query and schema

- [x] Make public searchable entity types explicit in the query; keep `type_order` only as an ordering key and retain active/private filtering.
- [x] Configure `normalized_text` with `C` collation and replace the exact/prefix B-trees with one active-only ordered B-tree; keep the active-only trigram GIN index and remove type names from both predicates.
- [x] Add and use the safe prefix upper-bound helper; implement parameterized scalar bounds with composable SQL, escaped `LIKE`, and exact-match exclusion.
- [x] Replace the cursor `OR` chain with one row-value comparison matching the B-tree key; verify generated SQL has no indexed-column casts.
- [x] Add focused prefix-helper, behavior, cursor, non-ASCII ordering, migration, and generated-SQL regression tests.
- [x] Add a reversible migration and model snapshot update; `Down` restores old indexes and predicates exactly, including `game`, and resets table options.

## Rebuild ownership and operations

- [x] Acquire cluster-wide ownership on the same pinned physical EF connection used for every Discovery rebuild mutation; recover on every acquisition and fail closed on owner-session loss.
- [x] Restrict staging `TRUNCATE` and all recovery, merge, status, and cleanup writes to the current lock owner; preserve logged staging and healthy-session failure behavior.
- [x] Cover two-worker contention, recovery after owner loss, double claim, same-session fencing, and no reconnect/cleanup after session loss with PostgreSQL integration tests.
- [x] Document and verify initial rollout and rollback gates that prevent old lock-unaware and new workers from overlapping.

## Evidence and validation

- [x] Strictly validate this change before implementation and again after implementation/task synchronization.
- [x] Capture actual EF SQL and selective exact/prefix/contains and deep-cursor plans under normal and forced generic planning on representative PostgreSQL data.
- [x] Record search index/table sizes, rebuild table statistics, representative latency/buffers, and production-like migration time; explain data-dependent outcomes.
