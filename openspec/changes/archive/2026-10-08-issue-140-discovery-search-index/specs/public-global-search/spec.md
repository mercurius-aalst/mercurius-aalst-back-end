## ADDED Requirements

### Requirement: Public search uses explicit eligibility and stable indexed ordering
Public global search MUST return only active User, Team, and Tournament search documents; Sponsor documents MUST NOT be returned. Searchability MUST be determined by the entity type itself and MUST NOT depend on `type_order`. Search MUST preserve exact-match, starts-with, then contains ranking and MUST page results in the deterministic order `(normalized_text, type_order, entity_id)` within each rank. `normalized_text` ordering MUST use PostgreSQL `C` collation, including the resulting byte/code-point order for non-ASCII labels.

#### Scenario: Sponsor and inactive documents stay out of results
- **WHEN** a matching search document is a Sponsor or is soft-deleted
- **THEN** public search MUST omit it regardless of its `type_order`

#### Scenario: Public entity eligibility does not depend on display order
- **WHEN** a matching active document is a User, Team, or Tournament
- **THEN** public search MUST consider it even if its ordering value changes
- **AND** an active Sponsor MUST remain ineligible even if its ordering value changes

#### Scenario: Search ranking and cursor order remain deterministic
- **WHEN** matching documents span exact, starts-with, and contains ranks
- **THEN** results MUST remain ordered exact before starts-with before contains
- **AND** each rank MUST order by normalized text, type order, then entity ID
- **AND** cursor paging MUST return each result once without skips

#### Scenario: Non-ASCII labels use C-collation ordering
- **WHEN** search results include `team zed` and `team émile`
- **THEN** `team zed` MUST precede `team émile`

### Requirement: Search projection indexes support exact, prefix, contains, and cursor queries
The database MUST provide one active-only B-tree over `(normalized_text, type_order, entity_id)` using `C` collation for `normalized_text`, and one active-only trigram GIN index over `normalized_text`. No Discovery search index predicate MUST encode entity-type names. The prefix pass MUST retain escaped `LIKE` matching and use parameterized, plain scalar lower and (when safely constructible) upper bounds on `normalized_text`; the `LIKE` predicate MUST remain the correctness guard. Cursor seeking MUST use one lexicographic comparison over the same ordered key and MUST not cast indexed columns.

#### Scenario: Prefix bounds remain indexable for reusable plans
- **WHEN** a starts-with query has a safely constructible upper bound
- **THEN** its SQL MUST compare `normalized_text >= lower` and `normalized_text < upper` directly
- **AND** both bounds MUST be parameters
- **AND** the query MUST retain escaped `LIKE` and exact-match exclusion

#### Scenario: Prefix upper bound cannot be safely incremented
- **WHEN** the prefix is empty or ends in a UTF-16 surrogate or the maximum code unit
- **THEN** the upper bound MUST be omitted
- **AND** the lower bound and `LIKE` predicate MUST still determine matching results

#### Scenario: Cursor seeks by the B-tree key
- **WHEN** a result page continues after a cursor
- **THEN** the query MUST compare `(normalized_text, type_order, entity_id)` lexicographically to the cursor key
- **AND** the indexed key columns MUST remain unwrapped and uncast
