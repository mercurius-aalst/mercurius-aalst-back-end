## 1. Discovery recovery lifecycle

- [x] 1.1 Replace elapsed-time recovery with one-time worker-startup recovery of interrupted running jobs, including stale staged-document cleanup.
- [x] 1.2 Preserve long-running job coalescing and propagate requested cancellation without persisting a failed status; retain genuine-failure cleanup and bounded errors.

## 2. Verification

- [x] 2.1 Add focused tests for immediate recovery regardless of age, unchanged running-job creation, cancellation recovery, and genuine failure behavior.
- [x] 2.2 Run focused and full validation, strict OpenSpec validation, and confirm that no migration or pending EF model change is required.
