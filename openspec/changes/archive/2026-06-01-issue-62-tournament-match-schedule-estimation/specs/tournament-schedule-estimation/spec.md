## ADDED Requirements

### Requirement: Admin tournament schedule configuration
Admins MUST be able to create and update scheduled tournaments with a planned tournament start time, an average single-game duration, and a break duration between rounds.

#### Scenario: Create tournament with valid schedule fields
- **WHEN** an admin creates a tournament with valid scheduling configuration
- **THEN** the tournament stores the configured planned start, single-game duration, and round break duration

#### Scenario: Reject invalid schedule fields
- **WHEN** an admin creates or updates a tournament with missing, zero, negative, or overflowing schedule values
- **THEN** the API rejects the request with a validation response

#### Scenario: Block schedule edits after generation
- **WHEN** match generation has started for a tournament
- **THEN** schedule configuration cannot be changed unless a deliberate rescheduling rule is introduced

### Requirement: Estimated match schedule generation
The system MUST assign estimated start and end times to generated matches using tournament schedule configuration and match format.

#### Scenario: Generate estimated match times
- **WHEN** an admin starts a scheduled tournament
- **THEN** generated matches receive deterministic estimated start and end times derived from tournament start, match format, finals format, and round breaks

#### Scenario: Apply format-based duration
- **WHEN** a generated match uses Best of 1, Best of 3, or Best of 5
- **THEN** its estimated duration equals the configured single-game duration multiplied by 1, 3, or 5 respectively

#### Scenario: Calculate tournament estimated end
- **WHEN** match schedules are generated
- **THEN** the tournament response includes an estimated end time equal to the end of the final scheduled match

### Requirement: Schedule data in API responses
The API MUST return schedule configuration and generated estimates in the game and match responses needed by the redesigned front-end.

#### Scenario: Read game schedule fields
- **WHEN** a client reads a game list or game detail response
- **THEN** the response includes the tournament schedule fields required to display planned timing

#### Scenario: Read match schedule fields
- **WHEN** a client reads generated matches through game detail or match detail responses
- **THEN** each match includes its estimated start and end times
