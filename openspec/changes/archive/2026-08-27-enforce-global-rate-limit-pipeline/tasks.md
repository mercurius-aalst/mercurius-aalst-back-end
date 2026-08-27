## 1. Middleware Ordering

- [x] 1.1 Invoke the existing global rate limiter after authentication and before authorization.
- [x] 1.2 Invoke Imageflow and its existing default static-file handling after the security pipeline in API host composition.

## 2. Focused Verification

- [x] 2.1 Add in-memory pipeline tests for anonymous protected and authenticated wrong-role requests exhausting the global limiter before authorization.
- [x] 2.2 Add an in-memory pipeline test that verifies an exhausted `/images` request is rejected before terminal image handling.
- [x] 2.3 Run focused and required repository validation, strict OpenSpec validation, and diff checks.
