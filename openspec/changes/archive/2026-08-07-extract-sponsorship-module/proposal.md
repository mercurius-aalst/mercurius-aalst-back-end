## Why

Sponsor metadata, sponsor placement, and their HTTP endpoints are still implemented in the API
host despite the existing Sponsorship contracts project. This leaves Competition coupled to host
implementation details and prevents Sponsorship from owning its lifecycle facts and persistence
configuration.

## What Changes

- Extract Sponsor metadata, validation, read models, endpoints, EF configuration, and application
  services into the Sponsorship module.
- Make Sponsorship the owner of game sponsor placement, including placement context and display
  metadata, while treating `game_id` only as an external Competition reference.
- Route existing sponsor endpoints through `MapSponsorshipModule` and keep the current game sponsor
  placement route implemented through the Competition-to-Sponsorship contract.
- Publish SponsorCreated, SponsorUpdated, SponsorDeleted, and GameSponsorPlacementChanged facts
  through the existing durable module eventing boundary.
- Separate sponsor metadata from file storage by consuming Media contracts rather than Media
  implementation types.
- Preserve current table names, foreign-key constraints, HTTP routes, authorization metadata,
  validation outcomes, and request/response JSON shapes.

## Capabilities

### New Capabilities

- `sponsorship-module-boundary`: Defines Sponsorship ownership, explicit Competition and Media
  dependencies, persistence integration, endpoint composition, and placement lifecycle behavior.

### Modified Capabilities

- `module-eventing`: Extends durable module event publication to Sponsorship lifecycle and game
  sponsor placement facts.

## Impact

The Sponsorship implementation and contracts projects, API composition root, shared EF model
configuration, existing sponsor endpoints and DTOs, Competition's sponsorship adapter, Media
contract use, module eventing registrations, and Sponsorship-focused tests are affected. No public
endpoint, JSON contract, authorization rule, database table, or column is removed or renamed.
