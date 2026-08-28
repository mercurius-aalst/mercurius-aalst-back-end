## Why

Image upload validation and file-system storage are currently split between the API host and the
Teams module. Competition and Sponsorship depend on a temporary host adapter, so Media.Contracts
does not yet represent a real module boundary. Centralizing this responsibility prevents the three
business modules from diverging on validation, encoding, deletion, and storage safety.

## What Changes

- Move image validation, WebP encoding, storage-key generation, and idempotent image deletion into
  `Mercurius.Modules.Media`.
- Make `IMediaModule` the Media implementation boundary consumed by Teams, Competition, and
  Sponsorship.
- Remove the host file-service registrations and temporary Media adapter after consumers are
  composed with the Media module.
- Retain the existing image paths, file-size and content-type validation behavior, and Imageflow
  middleware hosting so public routes and JSON remain unchanged.

## Capabilities

### New Capabilities

- `media-storage-boundary`: Defines Media ownership of validated image storage and the contract
  that business modules use to create and delete image assets.

### Modified Capabilities

- None.

## Impact

- Affected code: Media contracts and implementation, API composition, Teams logo storage, and the
  legacy host file services.
- Dependencies: the Media implementation will own the Imageflow encoding dependency; the API host
  continues to own Imageflow HTTP middleware configuration.
- APIs: no route, authorization, request, response, or JSON-shape changes.
- Persistence: no schema or data migration changes.
