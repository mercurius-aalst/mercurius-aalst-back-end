## ADDED Requirements

### Requirement: User identity keys
The API data store MUST keep `Users.Id` as the primary key and MUST enforce unique `Auth0UserId` values for stored users.

#### Scenario: User primary key remains Id
- **WHEN** the user model is applied to the database
- **THEN** `Users.Id` is the primary key

#### Scenario: Auth0 ID is unique
- **WHEN** the user model is applied to the database
- **THEN** the database enforces unique `Auth0UserId` values

### Requirement: Active user identity values are unique
The API data store MUST enforce unique non-null `Username`, `NormalizedUsername`, and `Email` values for active users.

#### Scenario: Active username is unique
- **WHEN** the user model is applied to the database
- **THEN** the database enforces unique non-null `Username` values where `IsDeleted` is false

#### Scenario: Active normalized username is unique
- **WHEN** the user model is applied to the database
- **THEN** the database enforces unique non-null `NormalizedUsername` values where `IsDeleted` is false

#### Scenario: Active email is unique
- **WHEN** the user model is applied to the database
- **THEN** the database enforces unique non-null `Email` values where `IsDeleted` is false

#### Scenario: Optional identity values may be absent
- **WHEN** incomplete active users do not have usernames or email snapshots
- **THEN** the database allows multiple rows with null optional identity values

#### Scenario: Deleted users do not reserve optional identity values
- **WHEN** a user is soft-deleted
- **THEN** its optional username, normalized username, and email values do not block active users from using those values
