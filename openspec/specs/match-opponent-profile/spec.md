# match-opponent-profile Specification

## Purpose

Defines the match-scoped opponent profile endpoint that lets a matched individual or opposing team captain read the permitted identity details of their assigned opponent.

## Requirements

### Requirement: Match opponent profile access
The API MUST expose `GET /v1/lan/matches/{id}/opponent-profile` to authenticated individuals and team captains who are assigned to that match. The response MUST contain only username, first name, last name, Discord ID, Steam ID, and Riot ID; it MUST NOT contain email or account-management fields.

#### Scenario: Individual reads their opponent profile
- **WHEN** an individual participant reads the opponent profile for their assigned match
- **THEN** the API returns the other individual participant's permitted profile details

#### Scenario: Team captain reads the opposing captain profile
- **WHEN** a team captain reads the opponent profile for their assigned match
- **THEN** the API returns the opposing team's captain's permitted profile details

#### Scenario: Team member cannot read match opponent profile
- **WHEN** a non-captain team member requests a match opponent profile
- **THEN** the request is denied

#### Scenario: Unrelated user cannot read match opponent profile
- **WHEN** an authenticated user who is not an individual participant or team captain requests a match opponent profile
- **THEN** the request is denied

#### Scenario: Match without an opposing user
- **WHEN** an authorized participant requests a profile for a match without an assigned opposing participant or captain
- **THEN** the endpoint returns 404

#### Scenario: Match and opponent must be active
- **WHEN** the match or opposing user does not exist or the opposing user is deleted
- **THEN** the endpoint returns 404
