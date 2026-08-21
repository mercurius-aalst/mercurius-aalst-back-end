## 1. Multipart Request Boundary

- [x] 1.1 Derive Kestrel total-body and FormOptions per-section limits from `FileStorage:MaxFileSizeInMB`, including the documented 64 KiB envelope.
- [x] 1.2 Preserve the existing Media service-level file-size validation.

## 2. Regression Coverage

- [x] 2.1 Add real Kestrel multipart tests for the exact file boundary, over-envelope HTTP 413 response, and no service invocation on rejection.
- [x] 2.2 Cover the retained downstream 5 MiB Media validation.

## 3. Validation And Contract Audit

- [x] 3.1 Run focused tests, restore, full build/test, format verification, strict OpenSpec validation, and EF pending-model checks.
- [x] 3.2 Confirm the host-wide Kestrel cap remains safe for current non-media bodies, and confirm no route, authorization, DTO, JSON, persistence, migration, or rate-limit contract drift; audit final diff/status.
