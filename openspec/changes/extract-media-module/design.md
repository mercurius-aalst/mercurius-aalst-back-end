## Context

The API host owns `IFileService`, `FileService`, `FileValidationService`, and a temporary
`LegacyMediaModuleAdapter`. Teams has an independent `TeamLogoStorage` implementation with the
same validation, Imageflow encoding, random file naming, and safe-delete rules. Competition and
Sponsorship already depend only on `IMediaModule`, but the service registered for that contract is
the temporary host adapter. Imageflow's HTTP middleware remains correctly owned by Platform/API
composition.

This phase must preserve existing image URLs (`images/<generated>.webp`), accepted content types,
size limits, endpoints, authorization, JSON, and the filesystem-backed deployment configuration.

## Goals / Non-Goals

**Goals:**

- Make Media the sole owner of image validation, encoding, local storage, generated storage keys,
  and idempotent deletion.
- Register a real Media implementation for `IMediaModule` so Teams, Competition, and Sponsorship
  depend only on Media.Contracts.
- Retire duplicate Teams storage and host-only Media bridge code.
- Verify validation, deletion safety, consumer composition, and unchanged API behavior.

**Non-Goals:**

- Changing HTTP image serving, Imageflow middleware, storage provider, public routes, request or
  response DTOs, authorization, or database schema.
- Introducing media persistence tables, external object storage, or media-management endpoints.
- Changing an owning module's responsibility for persisting its image URL or publishing events.

## Decisions

### Keep the existing stream-based Media contract

`IMediaModule.SaveImageAsync(MediaUpload, CancellationToken)` remains the contract. The host's
`IFormFile` stays at module endpoint/application boundaries and each consumer passes only content,
file metadata, and declared length to Media. This keeps ASP.NET request types out of the shared
contract and avoids a separate contract migration.

Alternative considered: add Teams-, Game-, and Sponsor-specific storage methods. Rejected because
all three share identical storage policy and do not need separate Media behavior today.

### Put the concrete storage pipeline in Media

Media will validate the upload before decoding it, create the configured storage directory, encode
the stream to lossless WebP, and return the same relative `images/<generated>.webp` reference used
today. The generated file name never uses untrusted client input. Its delete operation will accept
only safe image-relative references and will be a successful no-op for missing, blank, or unsafe
inputs.

Alternative considered: retain host `IFileService` behind an adapter. Rejected because that keeps
the Media implementation boundary transitional and leaves duplicate Teams behavior.

### Compose Media directly in the API host

`AddMediaModule(configuration)` will register the concrete Media implementation as
`IMediaModule`; the host will call it before registering business modules. Teams will consume the
contract directly, as Competition and Sponsorship already do. The host retains only HTTP pipeline
configuration for Imageflow.

Alternative considered: register per-module wrappers. Rejected because they add indirection with
no different behavior or isolation benefit.

### Test the module through its contract and composition

Focused Media tests will cover metadata validation and safe, idempotent deletion. Composition and
existing API contract tests will ensure the real module is registered and existing consumers retain
their observable behavior.

## Risks / Trade-offs

- [Image encoding behavior changes while moving code] → Retain the existing lossless WebP encoding
  and relative URL format; cover the module contract and existing API behavior.
- [Filesystem operations can leave a file after cancellation or encoding failure] → Delete the
  generated target if writing fails or cancellation is observed after the write begins.
- [Unsafe URL could escape the configured storage root] → Normalize paths, require the `images/`
  prefix, reject traversal, and verify the resolved target remains under the configured root.
- [A consumer accidentally references Media implementation code] → Keep implementation types
  internal and add project-reference/architecture coverage.

## Migration Plan

1. Add the Imageflow encoding dependency and concrete storage implementation to Media.
2. Register Media in API composition and remove the temporary host adapter and file-service
   registrations.
3. Replace Teams' private logo storage dependency with `IMediaModule`; Competition and
   Sponsorship require no contract change.
4. Add focused tests, run the required solution validation, and verify image middleware remains
   configured by the API host.

Rollback is a code-only revert: image files and persisted relative URLs remain compatible with the
prior implementation and require no data rollback.

## Open Questions

None. The phase preserves the existing local filesystem storage model and public URL convention.
