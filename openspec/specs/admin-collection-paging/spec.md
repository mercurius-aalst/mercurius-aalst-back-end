# admin-collection-paging Specification

## Purpose
Define bounded and deterministic paging for administrative user and tournament-registration collections.
## Requirements
### Requirement: Bounded administrative user collection
The API MUST support optional `page` and `pageSize` query parameters on the existing no-query administrative user collection. It MUST default omitted values to page 1 and page size 20, reject non-positive supplied values with a validation problem before user-service invocation, and cap a positive page size at 50. The response MUST remain the existing raw user JSON array and MUST preserve the existing route and authorization behavior.

#### Scenario: Default administrative user page
- **WHEN** an admin requests the user collection without `query`, `page`, or `pageSize`
- **THEN** the API returns at most the first 20 users as the existing raw JSON array

#### Scenario: Invalid user page rejected early
- **WHEN** an admin requests the no-query user collection with `page` or `pageSize` less than one
- **THEN** the API returns a validation problem before invoking the user service

#### Scenario: User page size capped
- **WHEN** an admin requests the no-query user collection with a positive `pageSize` greater than 50
- **THEN** the API returns at most 50 users

#### Scenario: User search remains cursor based
- **WHEN** an authenticated caller includes the `query` key on the user collection route with any `page` value
- **THEN** the API MUST retain the existing cursor search and existing search page-size semantics
- **AND** it MUST ignore `page`

### Requirement: Deterministic and overflow-safe administrative user paging
The no-query administrative user collection MUST order users by `NormalizedUsername` and then `Id`, apply paging before materialization, calculate the offset without integer overflow, and return an empty raw array when the requested offset cannot be represented by the query provider.

#### Scenario: Equal user names have stable page membership
- **WHEN** two or more administrative users have equal normalized usernames
- **THEN** their IDs deterministically break the ordering tie before paging

#### Scenario: User page offset overflows query-provider range
- **WHEN** an administrative user request specifies a positive page whose offset exceeds `Int32.MaxValue`
- **THEN** the API returns an empty raw array without an overflow exception
### Requirement: Bounded administrative registration collection
The API MUST support optional `page` and `pageSize` query parameters on the existing admin tournament-registration collection. It MUST default omitted values to page 1 and page size 20, reject non-positive supplied values with a validation problem before registration-service invocation, and cap a positive page size at 50. The response MUST remain the existing raw registration JSON array and MUST preserve the existing route and admin authorization behavior.

#### Scenario: Default administrative registration page
- **WHEN** an admin requests a tournament's registration collection without paging parameters
- **THEN** the API returns at most the first 20 registrations as the existing raw JSON array

#### Scenario: Invalid registration page rejected early
- **WHEN** an admin requests the registration collection with `page` or `pageSize` less than one
- **THEN** the API returns a validation problem before invoking the registration service

#### Scenario: Registration page size capped
- **WHEN** an admin requests the registration collection with a positive `pageSize` greater than 50
- **THEN** the API returns at most 50 registrations

### Requirement: Deterministic and overflow-safe administrative registration paging
The administrative tournament-registration collection MUST order registrations by `Kind`, `Status`, `CreatedAtUtc`, and then `Id`; apply paging before materialization and DTO enrichment; calculate the offset without integer overflow; and return an empty raw array when the requested offset cannot be represented by the query provider.

#### Scenario: Equal registration values have stable page membership
- **WHEN** registrations share kind, status, and creation timestamp
- **THEN** their IDs deterministically break the ordering tie before paging

#### Scenario: Registration page offset overflows query-provider range
- **WHEN** an administrative registration request specifies a positive page whose offset exceeds `Int32.MaxValue`
- **THEN** the API returns an empty raw array without an overflow exception
