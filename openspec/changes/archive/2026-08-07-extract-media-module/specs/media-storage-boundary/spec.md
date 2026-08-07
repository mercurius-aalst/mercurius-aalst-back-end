## ADDED Requirements

### Requirement: Media owns validated image storage
The Media module MUST be the sole implementation owner of image upload validation, WebP encoding,
configured local storage, generated image references, and image deletion. Business modules MUST
use `IMediaModule` from Media.Contracts rather than reference a Media implementation service.

#### Scenario: Business module stores an image through the contract
- **WHEN** Teams, Competition, or Sponsorship supplies an image stream and its metadata to
  `IMediaModule`
- **THEN** Media validates and stores the image and returns the relative image reference for the
  owning module to persist

#### Scenario: Host composition provides the Media implementation
- **WHEN** the API host registers application services
- **THEN** `IMediaModule` resolves to the Media module implementation without using a host adapter

### Requirement: Media preserves image upload validation
Media MUST reject a missing, empty, oversized, or unsupported image upload using the existing
validation behavior. Media MUST accept JPEG, PNG, WebP, and GIF content types within the configured
maximum size.

#### Scenario: Unsupported content type is supplied
- **WHEN** an upload declares a content type other than JPEG, PNG, WebP, or GIF
- **THEN** Media rejects the upload before persisting an image

#### Scenario: Valid image metadata is supplied
- **WHEN** an upload has a supported content type and a non-empty length no greater than the
  configured maximum size
- **THEN** Media accepts the upload for encoding and storage

### Requirement: Media returns and deletes safe image references
Media MUST return the existing `images/<generated>.webp` relative-reference format and MUST NOT
derive a storage key from untrusted client file names. Image deletion MUST be idempotent and MUST
not delete files outside the configured storage directory.

#### Scenario: Stored image reference is deleted twice
- **WHEN** deletion is requested twice for the same valid image reference
- **THEN** both requests complete successfully and no file outside Media storage is affected

#### Scenario: Unsafe image reference is supplied for deletion
- **WHEN** deletion is requested with a blank, traversal, or non-image reference
- **THEN** Media completes without deleting a file

### Requirement: HTTP image serving remains host infrastructure
The API host MUST retain Imageflow middleware configuration for `/images` using the configured
storage location. The Media module MUST NOT own HTTP middleware registration.

#### Scenario: Stored image is requested over HTTP
- **WHEN** a client requests an image under `/images`
- **THEN** the host Imageflow middleware serves it using the configured image storage location
