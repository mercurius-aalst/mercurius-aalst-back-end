## 1. OpenSpec and Brief

- [x] Create local issue brief documenting why global search is insufficient and the required authenticated user search contract.
- [x] Add OpenSpec proposal, design, and spec deltas for authenticated user search.

## 2. API Implementation

- [x] Add a privacy-safe user search result DTO.
- [x] Add user service lookup method with normalized query matching, bounded page size, and cursor continuation.
- [x] Map authenticated `GET /v1/lan/users?query={query}` on the existing users collection route.
- [x] Apply the existing search rate-limit setup to authenticated user search requests.
- [x] Add PostgreSQL indexes for authenticated user search matching and cursor continuation.

## 3. Verification

- [x] Add route authorization and service/privacy tests.
- [x] Add query translation and rate-limit metadata tests for authenticated user search.
- [x] Run `dotnet build LAN.API.sln`.
- [x] Run `dotnet test LAN.API.sln`.
- [x] Update this checklist after implementation and verification.
