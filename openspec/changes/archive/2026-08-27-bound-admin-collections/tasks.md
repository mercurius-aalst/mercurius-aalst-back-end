## 1. Admin collection endpoints

- [x] 1.1 Add scoped defaulted, capped, and early-invalid paging handling to the no-query admin user and admin registration HTTP handlers.
- [x] 1.2 Preserve user cursor-search branch selection and page-size behavior when the `query` key is present.

## 2. Query contracts and data access

- [x] 2.1 Thread page, page size, and cancellation tokens through the Identity admin user list service and decorators, applying deterministic overflow-safe database paging.
- [x] 2.2 Thread page and page size through Competition admin registration contracts and read model, applying deterministic overflow-safe paging before enrichment.

## 3. Tests and contract verification

- [x] 3.1 Add or update endpoint, service, fake, OpenAPI, ordering, overflow, cancellation, raw-array, and search-regression coverage within the two scoped APIs.
- [x] 3.2 Run focused and full validation, strict OpenSpec validation, and migration/model checks; audit the exact final scope.
