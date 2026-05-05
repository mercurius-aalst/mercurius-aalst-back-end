# Auth.Module

This project will host authentication orchestration for Mercurius:

- `AddAuthServices(...)` service registration entrypoint.
- `MapAuthEndpoints(...)` endpoint mapping entrypoint.

The immediate follow-up step is moving existing local auth endpoint mappings and auth service registrations from `MercuriusAPI` into this module without changing runtime behavior.
