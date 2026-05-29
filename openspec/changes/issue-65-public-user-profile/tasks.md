## 1. Public Profile Contract

- [ ] 1.1 Add a public user profile DTO with anonymous and authenticated visibility behavior.
- [ ] 1.2 Add a user service method for normalized public profile lookup.
- [ ] 1.3 Add `GET /v1/lan/public/users/{username}` with anonymous access and authenticated enrichment.

## 2. Privacy and Lookup Behavior

- [ ] 2.1 Validate and normalize username route input.
- [ ] 2.2 Exclude missing, deleted, incomplete, and username-less users with uniform 404 behavior.
- [ ] 2.3 Ensure anonymous responses omit platform identifiers and all private account fields.
- [ ] 2.4 Ensure authenticated responses include only the allowed platform identifiers beyond public names.

## 3. Regression Coverage

- [ ] 3.1 Add tests for anonymous and authenticated response shape.
- [ ] 3.2 Add tests for case-insensitive lookup, missing users, deleted users, and incomplete profiles.
- [ ] 3.3 Add privacy regression tests for email, Auth0 ID, timestamps, and deleted state.
- [ ] 3.4 Run `dotnet test` for the solution.
