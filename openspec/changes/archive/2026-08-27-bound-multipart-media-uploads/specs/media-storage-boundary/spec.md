## ADDED Requirements

### Requirement: Host bounds multipart image uploads before binding
The API host MUST derive its multipart image request limits from `FileStorage:MaxFileSizeInMB`. It MUST configure Kestrel to reject a total request body larger than the configured file size plus a 65,536-byte multipart envelope, and it MUST configure the multipart section-body limit to the configured file size. The Kestrel total-body limit applies to every request body received by the host, not only multipart requests; the section limit MUST NOT be treated as a total request limit. The host MUST apply the total request limit before Game, Sponsor, and Team upload handlers or their services execute, returning HTTP 413 for an over-limit request. Media MUST retain its existing file-size validation after binding.

#### Scenario: Exact configured file boundary is uploaded
- **WHEN** a Game, Sponsor, or Team multipart upload contains one supported file whose length equals `FileStorage:MaxFileSizeInMB` and current scalar form fields fit within the envelope
- **THEN** the request reaches the existing handler and service behavior

#### Scenario: Multipart request exceeds the envelope
- **WHEN** a multipart upload's total request body exceeds the configured file size plus 65,536 bytes
- **THEN** Kestrel returns HTTP 413 before the upload handler or service executes

#### Scenario: Direct Media caller exceeds file boundary
- **WHEN** a caller invokes Media with image metadata whose file length exceeds `FileStorage:MaxFileSizeInMB`
- **THEN** Media rejects the upload using its existing validation behavior

#### Scenario: Current non-media request body is within host cap
- **WHEN** the API receives a current non-media JSON request body smaller than the configured Kestrel limit
- **THEN** it retains its existing endpoint behavior
