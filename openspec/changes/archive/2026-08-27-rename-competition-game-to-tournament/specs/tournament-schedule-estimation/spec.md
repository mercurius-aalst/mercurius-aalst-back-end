## MODIFIED Requirements

### Requirement: Schedule data in API responses
The API MUST return schedule configuration and generated estimates in tournament and match
responses needed by the redesigned front-end. Single-game duration and match-format terminology
MUST remain unchanged.

#### Scenario: Read tournament schedule fields
- **WHEN** a client reads a tournament list or tournament detail response
- **THEN** the response includes the tournament schedule fields required to display planned timing

#### Scenario: Read match schedule fields
- **WHEN** a client reads generated matches through tournament detail or match detail responses
- **THEN** each match includes its estimated start and end times
