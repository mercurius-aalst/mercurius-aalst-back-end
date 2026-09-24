## ADDED Requirements

### Requirement: Tournament lifecycle concurrency conflicts

When a tournament lifecycle or configuration save fails because the tournament aggregate changed concurrently, the API MUST return a conflict response with a code that identifies the affected tournament type. A leaderboard conflict MUST keep code leaderboard_changed and message "The tournament or leaderboard changed. Refresh and try again." A non-leaderboard conflict MUST use code tournament_changed and message "The tournament changed. Refresh and try again." The failed operation MUST NOT persist its changes.

#### Scenario: Non-leaderboard lifecycle save conflicts

- **WHEN** another operation changes a non-leaderboard tournament while a lifecycle save is in progress
- **THEN** the API MUST return HTTP 409 with code tournament_changed and message "The tournament changed. Refresh and try again."
- **AND** the failed lifecycle change MUST NOT be persisted

#### Scenario: Leaderboard lifecycle conflict keeps its contract

- **WHEN** another operation changes a leaderboard tournament while a lifecycle save is in progress
- **THEN** the API MUST retain HTTP 409 with code leaderboard_changed and message "The tournament or leaderboard changed. Refresh and try again."
- **AND** the failed lifecycle change MUST NOT be persisted
