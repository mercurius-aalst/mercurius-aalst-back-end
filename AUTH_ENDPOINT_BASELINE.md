# Auth Endpoint Baseline (Phase 0)

This document captures the **current** local-auth endpoint contracts before moving auth mapping/DI into `Auth.Module`.

## Base route

`/api/v{version}/auth`

Versioning is URL-segment based (currently v1 via endpoint grouping metadata).

---

## 1) Register

- **Method/Route**: `POST /api/v{version}/auth/register`
- **Auth**: Required + role `admin`
- **Request body**: `LoginRequest`
  - `Username`
  - `Password`
- **Behavior**:
  - Creates a new local user.
  - Returns success with empty body on completion.
- **Errors (typical)**:
  - validation failures (e.g. username already exists)
  - authorization failures when caller is not admin

---

## 2) Login

- **Method/Route**: `POST /api/v{version}/auth/login`
- **Auth**: Anonymous allowed
- **Request body**: `LoginRequest`
  - `Username`
  - `Password`
- **Response body**: `AuthTokenResponse`
  - `Token`
  - `RefreshToken`
- **Behavior**:
  - Validates credentials
  - Enforces lockout policy
  - Issues internal JWT + refresh token

---

## 3) Refresh

- **Method/Route**: `POST /api/v{version}/auth/refresh`
- **Auth**: Anonymous allowed
- **Request body**: `RefreshTokenRequest`
  - `RefreshToken`
- **Response body**: `AuthTokenResponse`
  - `Token`
  - `RefreshToken` (rotated)
- **Behavior**:
  - Validates refresh token
  - Rotates refresh token
  - Issues a new internal JWT

---

## 4) Revoke

- **Method/Route**: `POST /api/v{version}/auth/revoke`
- **Auth**: Required
- **Request body**: `RevokeTokenRequest`
  - `RefreshToken`
- **Behavior**:
  - Removes/revokes refresh token from persistence
  - Returns success with empty body

---

## Regression invariants for extraction

When moving endpoint mapping + DI registration into `Auth.Module`, these invariants must not change:

1. Route templates remain identical.
2. Anonymous access remains only on `login` and `refresh`.
3. `register` keeps admin-role requirement.
4. Request/response contracts remain unchanged.
5. Internal JWT + refresh token behavior remains unchanged.
