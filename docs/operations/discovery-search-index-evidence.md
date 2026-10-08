# Discovery search-index evidence (issue #140)

Measured evidence for the Discovery search query, index, and rebuild changes. Raw captured SQL,
bound parameters, and every `EXPLAIN (ANALYZE, BUFFERS)` output are attached verbatim:

- [discovery-search-index-evidence/before.txt](discovery-search-index-evidence/before.txt) - baseline `1bff3f9`, pre-change source
- [discovery-search-index-evidence/after.txt](discovery-search-index-evidence/after.txt) - branch `codex/issue-140-discovery-search-index`, migration `20261008150454_DiscoverySearchQueryIndexOptimization`

The only edit to the raw logs is the local `data_directory` value, which is replaced with a neutral
placeholder. Captured SQL, parameter values, plan nodes, buffer counts, and timings are unmodified.

## Fixture and method

- PostgreSQL 17.11 on one local instance, reached at `127.0.0.1:5432`. Both trees ran against the same instance.
- Deterministic synthetic data, no production data: 105,000 rows (100,000 active + 5,000 soft-deleted).
  Active by type: user 40,000, team 30,000, tournament 20,000, sponsor 10,000. Sponsors are active on
  purpose: the old partial predicates excluded them, the new active-only predicates do not.
- Plans run on the SQL EF Core actually emitted, captured with a `DbCommandInterceptor`, not hand-written SQL.
  `normal` = the captured SQL with values bound; `reusable` = the same SQL under
  `SET plan_cache_mode = force_generic_plan` with a prepared statement. `ANALYZE` ran before every plan.
- Both trees were compiled from source copies; the after copy `src/` was byte-identical to the branch.

## Query plans (root `Buffers: shared hit`, `Execution Time`)

| Scenario | After, normal | After, forced generic | Before, normal |
|---|---|---|---|
| exact `tea` (1 row) | Index Scan, `= 'tea'`; 4 bufs, 0.020 ms | Index Scan, `= $2`; 4 bufs, 0.021 ms | Seq Scan + Sort; 2066 bufs, 13.27 ms |
| common prefix `tea%` (20 of 35,000) | Index Scan, `>= 'tea' AND < 'teb'`; 24 bufs, 0.036 ms | Index Scan, plain bounds; 24 bufs, 0.036 ms | (Parallel) Seq Scan + Sort; 2130 bufs, 33.15 ms |
| selective prefix `user-0000%` (9 rows) | Index Scan, plain bounds; 3 bufs, 0.020 ms | Index Scan, plain bounds; 3 bufs, 0.015 ms | Seq Scan + Sort; 2066 bufs, 13.59 ms |
| selective contains `%zzqx%` (30 rows) | GIN Bitmap + sort; 32 bufs, 0.085 ms | GIN Bitmap + sort; 32 bufs, 0.096 ms | Seq Scan + Sort; 2066 bufs, 14.00 ms |
| page 10 of the `tea%` range | Index Scan, row-value cursor in `Index Cond`; 24 bufs, 0.041 ms | Bitmap Index + Heap Scan; **1004 bufs, 16.371 ms** (exception below) | (Parallel) Seq Scan + Sort; 2130 bufs, 36.82 ms |
| near-end page of the `tea%` range | Index Scan, row-value cursor; 24 bufs, 0.044 ms | Bitmap heap; 24 bufs, 0.061 ms | (Parallel) Seq Scan + Sort; 2130 bufs, 34.23 ms |

Deep-page check: the near-end page of the 35,000-row range reads 24 buffers in both plan types, the same
order of magnitude as the 24-buffer first page, so the cursor seeks into the index instead of walking
preceding pages. Rows returned are identical before and after for every scenario (paging walks the whole
range in 1,751 pages of 20).

The starts-with `Index Cond` holds the plain scalar bounds under both plan types, and the sort key is
`normalized_text COLLATE "C"`: this is the accepted ordering change (for example `team zed` precedes
`team emile`). The exact and ordered starts-with plans need no sort.

## Residual exception: forced-generic mid-range page

`plan_generic:page10` is the one case that does not meet the "handful of buffers" bar, and it is reported
rather than hidden:

- Forced generic, cursor `tea-00178` (179 rows into the 35,000-row range): Bitmap Index + Bitmap Heap Scan
  of **34,821 actual rows** against a planner estimate of 167, **1004 buffers, 16.371 ms**.
- The same SQL, same parameters, with `enable_bitmapscan = off` (still forced generic):
  Index Scan with the row-value cursor in `Index Cond`, **24 buffers, 0.057 ms**. This is a diagnostic
  reproduction, not a proposed production configuration.
- The planner's own estimates are 525.32 (bitmap + sort) versus 564.98 (ordered index scan), so it chose
  the bitmap path on an estimate that is off by roughly 200x under a generic plan because the bound
  parameters are unknown at plan time.

Why it is accepted. It appears only under forced generic planning. With values visible (custom planning)
the planner picks the 24-buffer ordered seek for the same page, which is what the checked-in/default
configuration produces: `UseNpgsql` is called without `MaxAutoPrepare`/`AutoPrepareMinUsages`, and
Npgsql's default `MaxAutoPrepare=0` means no auto-prepare, so PostgreSQL builds a custom plan per
execution. The effective production connection string (for example a secret adding `MaxAutoPrepare` or
`plan_cache_mode`) was not inspected in this change; if generic plans ever become reachable, the smallest
justified mitigation is connection-level `plan_cache_mode = force_custom_plan`, not a query change.
Globally disabling bitmap scans is not appropriate because the contains pass legitimately uses one.
The independent performance review accepted this exception: exact, selective-prefix, and contains are
fast in both plan modes, the common 35,000-row prefix is nonselective by construction, and the near-end
page proves the deep seek works.

## Index footprint

| | before | after |
|---|---|---|
| table | 16 MB | 16 MB |
| indexes | 34 MB | 23 MB |
| total | 50 MB | 39 MB |

After: `PK_search_documents` 4304 kB, `IX_search_documents_entity_type_entity_id` 8840 kB,
`IX_search_documents_active_text` 7352 kB `(normalized_text, type_order, entity_id) WHERE is_deleted = false`,
`IX_search_documents_normalized_text_trgm` 2808 kB `WHERE is_deleted = false` - two search-specific
indexes, and no search index predicate names an entity type. The previous
`IX_search_documents_active_exact_order` and `IX_search_documents_active_prefix` are gone. The fall
from 34 MB to 23 MB is data-dependent and not an acceptance criterion: the new predicates cover 10,000
active sponsor rows the old partial predicates excluded, and a different distribution can produce a net
increase. Both runs bulk-loaded the identical fixture.

## Rebuild statistics and fillfactor

Real `SearchIndexRebuildService.RunNextAsync` runs, one rebuild per row, then an optional scoped
`VACUUM (FULL)` and a second rebuild on both trees (identical treatment; only `fillfactor` differs).

| | before (fillfactor 100) | after (fillfactor 90) |
|---|---|---|
| first rebuild `n_tup_upd` / `n_tup_hot_upd` | 100,000 / 1 | 100,000 / 2 |
| first rebuild HOT ratio | ~0.001% | ~0.002% |
| first rebuild duration | 14,162 ms | 14,299 ms |
| `VACUUM (FULL)` duration | 1,459 ms | 751 ms |
| second rebuild `n_tup_upd` / `n_tup_hot_upd` | 100,000 / 2 | 100,000 / 11,254 |
| second rebuild HOT ratio | 0.002% | 11.25% |
| second rebuild duration | 14,083 ms | 13,534 ms |

- The migration alone produces no HOT benefit. It only sets storage parameters and does not rewrite the
  heap, so the first rebuild after deploying still rewrites every indexed tuple: 100,000 updates with 1-2
  HOT updates, about 0%, identical before and after.
- `fillfactor = 90` becomes measurable only after pages adopt it. After an optional scratch
  `VACUUM (FULL)` rewrites the heap with the new setting, the same rebuild reaches 11,254/100,000 HOT
  (11.25%) versus 2/100,000 (0.002%) on the baseline. Even then about 89% still cannot be HOT because the
  merge rewrites `normalized_text` and the partial-index predicate.
- No `VACUUM (FULL)` is deployed or scheduled automatically; it stays an operator option. Rewriting the
  heap on this fixture took 751 ms at 105,000 rows / 40 MB total, which is scratch-benchmark data and
  must not be read as production sizing.
- Rebuild duration is not a reliable signal at this fixture size: repeat runs of the same rebuild
  measured 7.0 s, 14.2 s, 14.3 s, and 19.9 s with autovacuum competing after a 105k-row bulk load.
  **No rebuild-latency improvement is claimed.**
- The staging table is logically empty after every rebuild (`COUNT(*) = 0`). Its `n_dead_tup` is a
  `TRUNCATE` estimate and only a monitoring signal.

## Migration and validation

Migration `20261008150454_DiscoverySearchQueryIndexOptimization` drops the three old search indexes,
alters `normalized_text` to `COLLATE "C"`, creates the active ordered B-tree and the active trigram GIN
index, and sets the table storage parameters. `Down` restores the prior indexes and predicates exactly,
including the historical `game` entries.

- Applied to a database already one migration behind with 105,000 seeded rows: 452 ms in the recorded
  run (253-452 ms across runs of the same fixture).
- Up / Down / Re-Up against a real PostgreSQL 17 catalog passed: fresh Up 461 ms, Down 14 ms, Re-Up 13 ms.
- Full serial build: PASS, 0 errors, 9 `NU1900` offline-feed warnings. All 730 tests PASS
  (Discovery 31, others 699). Full format verify: PASS.
- Static concurrency review: PASS. Independent migration review: PASS. Independent performance review:
  PASS with the forced-generic page-10 exception documented above.

The rollback and first-rollout gates are recorded in
[discovery-search-index-rollout.md](discovery-search-index-rollout.md). The gates were verified by review;
no actual cluster deployment or production smoke was performed in this change.

## Limits

These are single-run measurements on one local instance with a synthetic fixture. They demonstrate plan
shape, buffer counts, and data-dependent outcomes, not production throughput. No universal production
latency figure and no guaranteed rebuild speedup follows from this data.
