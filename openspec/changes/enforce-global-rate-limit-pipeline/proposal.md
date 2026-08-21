## Why

The API's global limiter is currently placed after authorization, allowing rejected protected requests to avoid its fixed-window controls. Imageflow processing and cache handling also run before the global limiter, leaving an expensive public request path unprotected.

## What Changes

- Move global rate-limit enforcement to run after authentication but before authorization.
- Move Imageflow image processing and its cache handling downstream of global rate-limit enforcement.
- Add focused pipeline tests for protected anonymous traffic, protected wrong-role traffic, and image requests.

## Capabilities

### New Capabilities

- `global-rate-limit-pipeline`: API-wide fixed-window rate-limit enforcement across authorization failures and Imageflow image handling.

### Modified Capabilities

- None.

## Impact

- Affected code: API host middleware composition and Platform security/Imageflow extensions.
- Observable behavior: repeated protected anonymous or forbidden requests, and repeated `/images` requests, can receive the existing 429 response once their global bucket is exhausted.
- No route, DTO, policy, limit, partition-key, persistence, or dependency changes are introduced.
