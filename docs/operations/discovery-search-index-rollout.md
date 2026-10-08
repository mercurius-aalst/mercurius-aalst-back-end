# Discovery search-index rollout and rollback

The rebuild worker added in issue #140 coordinates through a PostgreSQL session advisory lock. Only binaries that acquire this lock are coordinated; an older worker can still recover jobs and clear staged data while a newer worker owns the lock. The first rollout and any rollback therefore require a worker handover.

## FAQ: applying the migration in Kubernetes

- **Do I need to run `psql` or other manual PostgreSQL commands?** No. Every schema change ships in the EF Core migration (`20261008150454_DiscoverySearchQueryIndexOptimization`): it drops the old search indexes, sets `normalized_text` to `COLLATE "C"`, creates the active ordered B-tree and trigram GIN index, and sets the table storage parameters. Applying EF migrations is enough; there is no separate SQL step.
- **Do I need to create the indexes or adjust collation by hand?** No. The migration owns the index and collation changes, and the model snapshot matches them. Confirm they exist afterward (see the initial rollout step below).
- **Who owns the advisory lock and the staging `TRUNCATE`?** The application does, at runtime. The rebuild worker acquires the session advisory lock on the pinned connection and truncates its own staging table while it holds that lock; no operator action is involved.
- **Is `VACUUM (FULL)` required?** No. It is optional and not automated. The migration sets `fillfactor` but does not rewrite the heap, so existing pages only gain HOT-update headroom after a heap rewrite. That optional rewrite is an operator decision for a maintenance window, not part of this rollout.
- **What is the ordering for the first deploy?** Stop the old pods, apply the migration once, then start the new pods, as detailed below. Do not rely on a plain rolling update for this first transition.
- **Anything to verify about the database route?** Confirm the connection reaches PostgreSQL directly or through session pooling. Transaction-pooling PgBouncer cannot hold a session advisory lock, so the rebuild worker must not run through it. This is an operator check of the deployed route, not a statement about any specific cluster.

## Preflight

- Confirm the effective `ConnectionStrings:MercuriusDB` host uses a session-compatible PostgreSQL connection. In the checked-in Helm chart, the backend assembles this connection string from the CloudNativePG application secret and does not deploy PgBouncer. If `backend.database.existingSecret` or another external database route is used, verify that it reaches PostgreSQL directly or uses session pooling. Transaction-pooling PgBouncer is incompatible with a session advisory lock; do not start rebuild workers through it.
- Confirm every lock-aware image in the rollout uses the same stable advisory-lock key.
- Confirm the migration is ready and the normal database backup/recovery process is current. The migration rebuilds the Discovery search indexes and changes `normalized_text` to `C` collation while preserving its values.
- Check the current rebuild job and allow any active legacy rebuild to finish before stopping the old deployment, when service operations allow it.

## Initial rollout

1. Use a controlled handover or maintenance window. Stop all backend pods running a lock-unaware version and wait until those processes have exited. Readiness changes alone do not stop hosted workers. Scaling the old backend deployment to zero is the fallback when no worker-only stop control exists.
2. Keep old workers stopped while the issue-140 migration runs. Verify the migration completed and the active text B-tree, active trigram GIN index, `C` column collation, and projection table settings are present.
3. Start the lock-aware backend deployment. Confirm one worker owns the rebuild lock at a time, other replicas skip rebuild work, and any orphaned `Running` job is recovered before new work proceeds.
4. Confirm a rebuild completes, the staging table is empty, and the public search checks pass before ending the handover window.

Do not use a normal rolling update for this first transition: an old pod can run recovery or cleanup while a new pod owns the lock.

## Later lock-aware deployments

Once every running backend version participates in the same advisory-lock protocol with the same key, normal rolling overlap is safe for rebuild ownership. Confirm nonowners continue polling and that only the owner recovers or mutates rebuild state.

## Rollback

1. Stop every lock-aware backend pod and wait until its rebuild worker has exited.
2. If the schema must be downgraded, apply the issue-140 `Down` migration while no rebuild worker is running. It restores the prior exact index and the historical `game` predicates on the prefix and trigram indexes.
3. Start the old backend only after all lock-aware workers are stopped. Never overlap old and new rebuild workers during rollback.

The PostgreSQL lock is released when its owner session ends. If an owner dies mid-rebuild, let a lock-aware worker reacquire ownership and recover the interrupted job before starting an older binary.
