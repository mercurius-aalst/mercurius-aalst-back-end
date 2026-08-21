## MODIFIED Requirements

### Requirement: Media returns and deletes safe image references
Media MUST return the existing `images/<generated>.webp` relative-reference format and MUST NOT
derive a storage key from untrusted client file names. Image deletion MUST be idempotent, MUST
not delete files outside the configured storage directory, and MUST treat blank, external,
default/static, traversal, non-image, and arbitrary references as non-owned no-ops.

#### Scenario: Stored image reference is deleted twice
- **WHEN** deletion is requested twice for the same valid image reference
- **THEN** both requests complete successfully and no file outside Media storage is affected

#### Scenario: Unsafe or non-owned image reference is supplied for deletion
- **WHEN** deletion is requested with a blank, external, default/static, traversal, non-image, or arbitrary reference
- **THEN** Media completes without deleting a file

## ADDED Requirements

### Requirement: Business mutations coordinate image lifecycle around commit
Teams, Competition, and Sponsorship MUST compensate a newly stored image when its owning mutation
fails or is cancelled before commit and MUST retire a replaced or deleted owned image only after
the related database and durable-business-event commit succeeds. Cleanup MUST use a non-cancelled
token, MUST be best effort, and MUST NOT replace the original pre-commit failure or turn a committed
mutation into a failure response.

#### Scenario: Owning mutation fails after image storage
- **WHEN** Media returns a newly stored image and the following mutation or commit fails or is cancelled
- **THEN** the owning module attempts to delete only that new reference with a non-cancelled token
- **AND** rethrows the original failure even when compensation also fails

#### Scenario: Replacement commits successfully
- **WHEN** a Game image, Sponsor logo, or Team logo replacement commits with a different owned reference
- **THEN** the owning module attempts to delete the previous unreferenced owned image after commit

#### Scenario: Post-commit deletion fails
- **WHEN** the database mutation commits but physical image deletion fails
- **THEN** the mutation remains successful
- **AND** the cleanup failure is logged for manual remediation

#### Scenario: Reference is blank or unchanged
- **WHEN** the candidate reference is blank or ordinally equal to the new current reference
- **THEN** the owning module does not request its deletion
