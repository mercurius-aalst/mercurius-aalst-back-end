## Why

Image uploads are currently rejected only after minimal-API form binding has read and buffered the multipart body. An authenticated caller can therefore consume substantially more request-body and temporary-storage resources than the configured image limit before Media rejects the file.

## What Changes

- Derive Kestrel's host-wide total request-body limit and form multipart section limit from the existing `FileStorage:MaxFileSizeInMB` setting.
- Reserve a fixed 64 KiB multipart envelope for the current non-file fields and multipart headers while retaining the configured image-size cap for each file section.
- Reject over-envelope multipart uploads with HTTP 413 before a Game, Sponsor, or Team upload handler invokes its service.
- Retain Media's existing exact file validation as defense in depth after binding.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `media-storage-boundary`: Bound HTTP multipart image uploads before form binding while preserving Media's validation boundary.

## Impact

- Affects API host Kestrel and form configuration, plus focused API and Media tests. Kestrel's total cap applies to every request body received by this host, not only multipart uploads; current JSON/request DTO contracts remain far below the 5 MiB plus 64 KiB cap.
- Game, Sponsor, and Team upload routes, authorization, DTOs, JSON shapes, image encoding, storage, rate limiting, database schema, and migrations remain unchanged.
- An upstream reverse proxy MUST enforce an equivalent request-body limit independently; this host limit begins only after the proxy forwards the request.
