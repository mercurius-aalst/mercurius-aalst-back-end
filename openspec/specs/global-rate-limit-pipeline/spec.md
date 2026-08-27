# global-rate-limit-pipeline Specification

## Purpose
TBD - created by archiving change enforce-global-rate-limit-pipeline. Update Purpose after archive.
## Requirements
### Requirement: Global rate limiting precedes authorization
The API host MUST authenticate each request before selecting its existing global rate-limit partition and MUST enforce the global limiter before authorization. Valid authenticated callers MUST continue to use the existing subject-claim partition, and callers without a valid authenticated identity MUST continue to use the existing remote-IP partition.

#### Scenario: Repeated anonymous protected request
- **WHEN** an anonymous caller repeats a request to a protected endpoint until its global fixed-window bucket is exhausted
- **THEN** the endpoint MUST return its normal authorization challenge before exhaustion and the existing 429 rate-limit response after exhaustion

#### Scenario: Repeated authenticated forbidden request
- **WHEN** an authenticated caller without the required role repeats a request to a protected endpoint until its global fixed-window bucket is exhausted
- **THEN** the endpoint MUST return its normal forbidden response before exhaustion and the existing 429 rate-limit response after exhaustion

### Requirement: Global rate limiting precedes Imageflow handling
The API host MUST invoke Imageflow processing and cache handling for `/images` only after global rate-limit enforcement.

#### Scenario: Repeated image request
- **WHEN** a caller repeats a request under `/images` until its global fixed-window bucket is exhausted
- **THEN** the existing 429 rate-limit response MUST be produced before terminal image processing or cache handling executes
