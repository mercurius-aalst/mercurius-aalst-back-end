## 1. Route Contract

- [ ] 1.1 Add `DELETE /v1/lan/users/{username}` to the existing admin user route group.
- [ ] 1.2 Reuse `IUserService.DeleteUserAsync(string username)`.
- [ ] 1.3 Keep `DELETE /v1/lan/users/{username}/account` available for compatibility.
- [ ] 1.4 Validate empty or malformed username input safely before lookup.

## 2. Authorization and Precedence

- [ ] 2.1 Confirm the new route inherits admin authorization.
- [ ] 2.2 Preserve `DELETE /v1/lan/users/{id:guid}` behavior.
- [ ] 2.3 Review route ordering for literal user subresources and future conflicts.

## 3. Regression Coverage

- [ ] 3.1 Add tests for admin username delete success and missing user 404.
- [ ] 3.2 Add tests for anonymous and non-admin rejection.
- [ ] 3.3 Add tests that GUID delete and username delete route precedence both work.
- [ ] 3.4 Run `dotnet test` for the solution.
