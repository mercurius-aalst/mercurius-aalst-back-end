## ADDED Requirements

### Requirement: Affected users receive their membership change event
The API MUST publish a membership-change event to the affected user's authenticated personal group as well as the current team group, without changing the existing event name or payload shape.

#### Scenario: A connected member is removed
- **WHEN** a committed member removal revokes the affected user's team-group connections
- **THEN** the API MUST publish the existing membership-change event to the current team group and that user's personal group
- **AND** the user MUST receive the event through their personal connection while remaining revoked from the team group

#### Scenario: A user leaves or joins a team
- **WHEN** a committed membership change affects a connected user
- **THEN** the user MUST receive the existing membership-change event through their personal group
- **AND** the event MUST continue to reach the current team group

#### Scenario: A connected team member's team is deleted
- **WHEN** a team deletion commits and the API revokes the team's realtime group
- **THEN** the API MUST notify every pre-deletion member through that member's authenticated personal group
- **AND** the API MUST NOT restore or retain any deleted team's group subscription
- **AND** the API MUST notify every user with an unexpired pending invite through that user's authenticated personal group using the existing invite-change event with a cancelled status
