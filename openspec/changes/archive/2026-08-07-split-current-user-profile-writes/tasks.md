## 1. Current User API Contract

- [x] 1.1 Add `PUT /v1/lan/users/me` and keep it authenticated.
- [x] 1.2 Change `GET /v1/lan/users/me` so it only reads existing current-user records.
- [x] 1.3 Keep `PATCH /v1/lan/users/me` update-only for existing current-user records.
- [x] 1.4 Keep `POST /v1/lan/users/me/complete-profile` update-only for existing current-user records.

## 2. Service Behavior

- [x] 2.1 Add a current-user creation service method that derives Auth0 identity from the authenticated subject.
- [x] 2.2 Ensure current-user reads do not create users, call Auth0 profile sync, or save database changes.
- [x] 2.3 Ensure current-user creation rejects existing users and current-user update rejects missing users.
- [x] 2.4 Ensure complete-profile updates existing users and rejects missing users.

## 3. Regression Coverage

- [x] 3.1 Add tests for read-only `GET /me`, missing-user 404 behavior, and absence of database writes.
- [x] 3.2 Add tests for `PUT /me` creation and existing-user rejection.
- [x] 3.3 Add route tests for authenticated `PUT /me`.
- [x] 3.4 Add tests for `POST /me/complete-profile` updating existing users and not creating missing users.
- [x] 3.5 Run `dotnet test LAN.API.sln`.
