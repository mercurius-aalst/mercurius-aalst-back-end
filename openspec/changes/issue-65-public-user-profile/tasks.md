## 1. Public Profile Contract

- [x] 1.1 Add a public user profile DTO with the public response shape.
- [x] 1.2 Add a user service method for normalized public profile lookup.
- [x] 1.3 Add `GET /v1/lan/public/users/{username}` with anonymous access.

## 2. Privacy and Lookup Behavior

- [x] 2.1 Validate and normalize username route input.
- [x] 2.2 Exclude missing, deleted, incomplete, and username-less users with uniform 404 behavior.
- [x] 2.3 Ensure public responses include platform identifiers and omit all private account fields.
- [x] 2.4 Ensure the response shape is the same for anonymous and authenticated callers.

## 3. Regression Coverage

- [x] 3.1 Add tests for public response shape.
- [x] 3.2 Add tests for case-insensitive lookup, missing users, deleted users, and incomplete profiles.
- [x] 3.3 Add privacy regression tests for email, Auth0 ID, timestamps, and deleted state.
- [x] 3.4 Run `dotnet test` for the solution.
