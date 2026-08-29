# Match result lifecycle

## ADDED Requirements

### Requirement: Match lifecycle projection

The API MUST expose a privacy-safe lifecycle state, ended-confirmation flags, score reports, and server-provided deadlines in the match projection. The projection MUST retain participant and bracket fields already returned by the match endpoint and MUST NOT expose moderation subjects or private notes.

#### Scenario: Public match is awaiting confirmations

- **WHEN** an anonymous caller requests a match whose participants have not both confirmed that play has ended
- **THEN** the API MUST return the match with the lifecycle state and confirmation flags
- **AND** it MUST omit authenticated identity details

#### Scenario: Client refreshes after a deadline

- **WHEN** a match is read after a server deadline has elapsed
- **THEN** the API MUST return the effective authoritative state
- **AND** any client countdown MUST be advisory only

### Requirement: Participant end confirmation

An authenticated individual participant or team captain MUST be able to confirm that their side has ended the match. The API MUST reject users who are not a participant or captain, reject repeated/expired confirmations, and MUST allow score submission only after both sides have confirmed.

#### Scenario: Both sides confirm

- **WHEN** each eligible side confirms ended
- **THEN** the lifecycle MUST enter AwaitingScore
- **AND** the API MUST allow score reports from either eligible side

#### Scenario: Unauthorized confirmation

- **WHEN** a signed-in user who does not own either side confirms ended
- **THEN** the API MUST reject the command
- **AND** the match MUST remain unchanged

### Requirement: Score consensus and correction

The API MUST accept non-negative, match-format-valid score reports only after both ended confirmations. The first report MUST start a five-minute server deadline for the other side. A matching report MUST complete the result and advance the bracket transactionally. A differing report MUST enter a correction window of five minutes; if the window expires without agreement, the lifecycle MUST enter AdminResolutionRequired.

#### Scenario: Matching reports complete

- **WHEN** both eligible sides report the same valid score before the deadline
- **THEN** the result MUST be completed once
- **AND** winner/loser and linked next-match assignments MUST be advanced in the same transaction

#### Scenario: First report is auto-accepted at the exact deadline

- **WHEN** only one eligible side has submitted a valid score and the server clock reaches the five-minute deadline
- **THEN** the first score MUST become official and advance the bracket atomically
- **AND** repeating deadline processing MUST NOT create a second result

#### Scenario: Differing reports require correction

- **WHEN** eligible sides report different valid scores
- **THEN** the lifecycle MUST be Disputed
- **AND** the correction deadline MUST be returned in the projection

#### Scenario: Unresolved correction requires admin

- **WHEN** the correction deadline expires without matching reports
- **THEN** the lifecycle MUST be AdminResolutionRequired
- **AND** player score commands MUST be rejected
- **AND** the assigned admin MUST receive a durable resolution-required notification

#### Scenario: Each side has one correction

- **WHEN** a disputed match is within its correction window
- **THEN** each side MAY replace its own report at most once
- **AND** a second correction by the same side MUST be rejected without changing the report

### Requirement: Forfeit

An eligible participant MAY forfeit their own side through an explicit command. An admin MAY forfeit either side. A forfeit MUST be irreversible by normal participants, MUST complete the match with the other side as winner, and MUST advance the bracket transactionally.

#### Scenario: Participant forfeits

- **WHEN** an eligible participant confirms a forfeit command
- **THEN** the match MUST be marked Forfeited
- **AND** the opposing side MUST be the winner

### Requirement: Administrative resolution and reversal

An authenticated admin MUST be able to resolve a disputed or admin-resolution-required match with a final score. An authenticated admin MUST be able to reverse a completed or forfeited result only when linked next matches have no result. The API MUST reject a reversal with an actionable reason when downstream play has started or completed.

#### Scenario: Admin resolves dispute

- **WHEN** an admin submits a valid final score for a disputed match
- **THEN** the match MUST complete and bracket advancement MUST be transactional
- **AND** the projection MUST identify the completed lifecycle state

#### Scenario: Reversal is blocked by downstream result

- **WHEN** an admin requests reversal and a linked next match has a score, winner, loser, or forfeit result
- **THEN** the API MUST reject the request with a clear reason
- **AND** no match or bracket state MUST change

### Requirement: Transactional and authorized transitions

Every lifecycle mutation MUST revalidate the current match state, participant ownership, server deadline, and tournament status immediately before saving. Bracket advancement and reversal MUST commit atomically with the match result. Unauthorized or invalid commands MUST NOT partially mutate persisted state.

#### Scenario: Invalid mutation is atomic

- **WHEN** a lifecycle command fails authorization, deadline, or state validation
- **THEN** the API MUST return a machine-readable failure reason
- **AND** no match or linked bracket state MUST be persisted
