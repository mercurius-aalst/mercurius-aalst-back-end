## 1. Media module implementation

- [x] 1.1 Add Media's concrete storage dependencies and register its `IMediaModule` implementation.
- [x] 1.2 Implement centralized upload validation, lossless WebP storage, safe generated references,
  cancellation cleanup, and idempotent deletion inside Media.

## 2. Consumer and host migration

- [x] 2.1 Replace Teams' private logo storage with the Media contract while preserving its endpoint
  behavior.
- [x] 2.2 Compose Media in the API host and remove the legacy host file services and temporary
  Media adapter.
- [x] 2.3 Confirm Competition and Sponsorship continue to consume Media.Contracts only and retain
  their existing image behavior.

## 3. Test coverage

- [x] 3.1 Add focused Media tests for upload metadata validation and safe idempotent deletion.
- [x] 3.2 Add or update composition and architecture coverage for real Media registration and
  consumer dependency boundaries.

## 4. Verification

- [x] 4.1 Run targeted Media and affected API contract tests.
- [x] 4.2 Run required repository validation and review API, OpenAPI, route, DTO/JSON, database,
  configuration, and image-serving impact.
