## 1. Contract and read model

- [x] 1.1 Add the authenticated paged current-user pending roster selection endpoint and DTOs.
- [x] 1.2 Query only actionable pending selections for scheduled tournaments with deterministic paging and total count.

## 2. Decline and replacement behavior

- [x] 2.1 Add the authenticated idempotent decline endpoint for an owned pending roster member.
- [x] 2.2 Remove only the declining roster member and keep the team registration pending.
- [x] 2.3 Preserve unchanged confirmation responses when a captain replaces the incomplete roster.
- [x] 2.4 Prevent activation unless the roster still has the tournament's exact required size.

## 3. Realtime updates

- [x] 3.1 Attempt committed Confirmed and Declined roster changes to the existing user and team targets without failing committed member responses when advisory delivery fails.
- [x] 3.2 Attempt Pending and Withdrawn replacement changes only for selections whose actionable state changed.

## 4. Verification

- [x] 4.1 Cover endpoint routing, authentication, paging validation, and response shape.
- [x] 4.2 Cover ownership, idempotency, state preservation, eligibility release, replacement preservation, and activation invariants.
- [x] 4.3 Cover notification filtering/paging and best-effort post-commit realtime events.
- [x] 4.4 Run focused tests, solution build, and strict OpenSpec validation.
