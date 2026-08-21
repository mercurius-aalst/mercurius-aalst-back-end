## Context

The Game, Sponsor, and Team image endpoints bind `IFormFile` or a `[FromForm]` DTO. The existing Media implementation then rejects a file whose length exceeds `FileStorage:MaxFileSizeInMB`, but form binding has already read and potentially spooled that file. The configured maximum is currently 5 MiB.

`FormOptions.MultipartBodyLengthLimit` applies to each multipart section rather than the entire HTTP request. It therefore preserves the individual file boundary but cannot by itself stop a request containing oversized aggregate multipart content. Endpoint request-size metadata is not a Kestrel total-body boundary for this minimal-API application, so it is not used as the primary control. Kestrel's server limit applies to every request body on this host; the current JSON DTO and scalar form contracts do not carry binary payloads and are safely below 5 MiB plus 64 KiB.

## Goals / Non-Goals

**Goals:**

- Reject multipart request bodies that exceed one configured media file plus a small current-form envelope before endpoint binding and Media invocation.
- Keep one exact configured limit for every multipart file section and retain Media's existing post-binding validation.
- Preserve existing accepted upload forms and API contracts.

**Non-Goals:**

- Streaming uploads, custom multipart parsing, temporary-file management, reverse-proxy configuration, or new storage/options abstractions.
- Changing the per-file content-type, empty-file, image re-encoding, authorization, route, DTO, rate-limit, or persistence behavior.

## Decisions

- **Use the existing FileStorage maximum as the sole source.** Convert `FileStorage:MaxFileSizeInMB` to bytes once during API-host composition. Invalid non-positive values fail startup instead of silently creating an ineffective host limit. No additional configuration section is introduced.

- **Set a total host-wide Kestrel request limit of `fileBytes + 65,536`.** Kestrel's `MaxRequestBodySize` is the reliable server-level total-body limit that applies before form binding, including for non-multipart requests. The fixed 64 KiB envelope covers the current scalar form values and multipart headers while preventing a second large part from being buffered. With the current 5 MiB configuration, the total is 5,308,416 bytes.

- **Set `FormOptions.MultipartBodyLengthLimit` to `fileBytes`.** This is deliberately a per-section limit, not a total-request claim. It prevents one file section from exceeding Media's configured file maximum even when a request falls below Kestrel's total limit.

- **Retain service validation.** `FileSystemMediaModule` continues to compare the uploaded file length to the configured 5 MiB limit, protecting direct module calls and any future host composition that does not use this API configuration.

## Risks / Trade-offs

- **[A future form adds more than 64 KiB of scalar values or headers]** → Its multipart request will receive 413 until the documented envelope is intentionally reviewed and increased.
- **[An upstream proxy accepts larger bodies]** → The proxy can still receive traffic before Kestrel; deployment configuration must enforce an equivalent limit at that boundary.
- **[The host-level Kestrel limit applies to another future large request]** → A deliberate large-request feature must explicitly review the global host limit instead of silently bypassing this media protection.

## Migration Plan

1. Deploy the host configuration and tests together; no database or client migration is required.
2. Existing uploads with a file at or below 5 MiB and current form fields remain accepted.
3. Rollback removes the host/form limit configuration; Media's existing service-level validation remains active.

## Open Questions

None.
