## 1. Route request contracts

- [x] 1.1 Add narrow request DTOs and validation for game lifecycle-state updates, team invitation creation, and roster-member confirmation updates.
- [x] 1.2 Preserve existing service operations and response DTO JSON shapes while dispatching the new request contracts.

## 2. Identity and Teams endpoints

- [x] 2.1 Replace the current-user profile completion and Identity action routes with their canonical profile and request-resource routes.
- [x] 2.2 Replace team leave, invitation creation and response, and logo upload routes with the specified membership, invitation, and logo resource routes.

## 3. Competition endpoints

- [x] 3.1 Replace Games lifecycle action routes with the lifecycle-state resource endpoint and remove all four action routes.
- [x] 3.2 Replace individual registration, eligibility, and roster-confirmation action routes; retain the team-roster replacement and proposed-roster eligibility behaviors at their canonical routes.

## 4. Contract coverage

- [x] 4.1 Update module route and authorization tests for every canonical Phase 16 route and every intentionally removed route.
- [x] 4.2 Update generated OpenAPI contract coverage for canonical and removed routes without changing response serialization contracts.

## 5. Verification

- [x] 5.1 Run OpenSpec validation and the focused route, authorization, OpenAPI, and serialization tests.
- [x] 5.2 Run the required performance, clean-code, and security audits and resolve all high- and medium-severity findings in scope.
- [ ] 5.3 Run `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`; record API/OpenAPI and contract impacts for the phase PR.
