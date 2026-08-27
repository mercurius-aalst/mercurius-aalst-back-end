## 1. Narrow Lifecycle Model

- [x] 1.1 Retain only next-attempt and dead-letter state on the outbox entity and configure one pending-dispatch index.
- [x] 1.2 Revise the undeployed hardening migration, designer, and model snapshot without lease or retention artifacts.

## 2. Isolated Dispatch And Retry

- [x] 2.1 Select deterministic eligible identifiers without tracking, then load and process each message independently.
- [x] 2.2 Record deterministic capped retries and dead-letter the fifth failed attempt while preserving cancellation behavior.
- [x] 2.3 Restore the single worker's fixed batch and two-second idle polling with no eventing configuration section.

## 3. Focused Verification

- [x] 3.1 Cover later-message persistence after an earlier failure, immediate retry deferral, fifth-attempt dead-lettering, and cross-batch poison non-starvation.
- [x] 3.2 Verify migration/runtime model fields and the single pending index contain no lease or retention artifacts.
- [x] 3.3 Run focused tests, full restore/build/test/format validation, strict OpenSpec validation, EF pending-model checks, and forbidden-artifact review.
