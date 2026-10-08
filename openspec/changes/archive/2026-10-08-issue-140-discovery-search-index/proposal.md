# Optimize Discovery search indexes and rebuild ownership

Issue #140 fixes stale and ineligible Discovery search indexes, makes exact, prefix, contains, and cursor-paged searches use the intended access paths, and prevents concurrent API replicas from recovering or mutating the same rebuild. The existing public endpoint, privacy rules, and exact → prefix → contains ranking remain intact. The accepted visible change is deterministic `C`-collation ordering for non-ASCII labels.

The search query will explicitly allow public users, teams, and tournaments. One active-only B-tree will cover exact matching, prefix ranges, ordering, and cursor seeking; one active-only trigram GIN index will cover contains searches. Prefix bounds will be parameterized scalar comparisons, with `LIKE` retained as the correctness check, and the cursor will compare the ordered key tuple.

Rebuild ownership will use a PostgreSQL session advisory lock on the pinned EF connection that performs all Discovery rebuild work. Each acquired owner recovers interrupted jobs first. Losing that session ends the cycle without reconnecting to continue writes. Staging remains logged and may be truncated only while ownership is held. The initial rollout and rollback will gate old, lock-unaware workers from overlapping with new workers.

## Scope

- Correct the public-search eligibility predicate and active-only index definitions.
- Make the accepted `C`-collation result order, prefix range, and cursor behavior explicit and regression-tested.
- Add a reversible migration whose `Down` restores the prior schema and prior index predicates exactly.
- Fence all rebuild recovery, staging, merge, status, and cleanup work across replicas on one physical PostgreSQL session.
- Document and verify safe first rollout/rollback and representative query/rebuild performance.

Soft-delete retention, sponsor visibility, public API shape, result ranking, per-module indexes, caching, and fuzzy matching are outside this change.
