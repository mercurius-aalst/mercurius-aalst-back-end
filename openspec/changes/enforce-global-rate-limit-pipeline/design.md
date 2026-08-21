## Context

The API host currently registers Imageflow before `UseSecurityPipeline`, while the security extension invokes authorization before rate limiting. Authentication must remain before the limiter because its global partition key uses the validated `sub` claim; authorization and Imageflow can terminate requests and therefore must follow it.

## Goals / Non-Goals

**Goals:**

- Apply the existing global fixed-window limiter to requests before authorization can emit 401 or 403.
- Apply that limiter before Imageflow can process or serve `/images` requests.
- Retain authenticated `sub` and anonymous remote-IP partition behavior, existing rate-limit policies, rejection payload, and the remaining host pipeline behavior.

**Non-Goals:**

- Changing rate-limit limits, policies, queueing, response format, endpoint metadata, authentication, or authorization behavior before exhaustion.
- Adding forwarded-header/proxy support, distributed rate limiting, new middleware abstractions, or changing image storage/cache configuration.

## Decisions

### Reorder only the existing middleware registrations

`UseSecurityPipeline` will invoke authentication, then the existing rate limiter, then authorization. The API host will invoke Imageflow after that pipeline. This is the smallest correction: valid authenticated callers still populate `HttpContext.User` before partition selection, while authorization and Imageflow are both downstream of the global limiter.

Alternative considered: split the security extension or introduce a separate image pipeline abstraction. Rejected because the host needs only one ordering change and those abstractions would not add a boundary or behavior.

### Keep the default static-file call paired with Imageflow

The default static-file registration remains inside `UseImageflowWithCaching`, so it moves with Imageflow. No default asset behavior is otherwise established in the host, and separating it would expand the change without a demonstrated need.

### Test execution order through a minimal in-memory pipeline

Focused tests will execute the existing security extension with protected endpoints and a terminal image-handler marker. They will assert that the second anonymous protected request and second authenticated wrong-role request are rejected with 429, and that a second `/images` request is rejected before its terminal image handling runs.

## Risks / Trade-offs

- [Anonymous callers behind a proxy can share a remote-IP bucket] → Preserve the existing partition rule; proxy/header configuration is deployment-specific and out of scope without deployment evidence.
- [In-memory fixed-window state is instance-local] → Preserve the current single-instance behavior; distributed coordination is out of scope.
- [Default static files become rate limited] → They already move with Imageflow's existing extension and no host-owned default assets require a separate placement.

## Migration Plan

1. Deploy the middleware-order change with existing rate-limit configuration.
2. Monitor 429 rates for protected traffic and `/images` requests.
3. Roll back by restoring the prior two registrations if an unexpected operational issue is found; no data migration is required.

## Open Questions

- None.
