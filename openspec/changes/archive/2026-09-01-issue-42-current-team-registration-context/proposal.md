## Why

The current authenticated tournament-registration read model only populates
`ActiveTeamRegistration`. A roster member who confirms while another member remains pending is
still part of a pending team registration, but the response loses the member's team ownership and
roster context. The front end then cannot explain that roster changes remain captain-owned and may
incorrectly present the member as unaffiliated.

## What Changes

- Add a `CurrentTeamRegistration` field to the authenticated current-user registration response.
- Populate it with the caller's team registration whenever the caller is a roster member, including
  pending-confirmation and active statuses.
- Keep `ActiveTeamRegistration` in the response with its existing active-only semantics for
  compatibility with current consumers.
- Keep the field restricted to the authenticated current-user endpoint; public tournament
  projections continue to expose active registrations only and remain privacy-safe.

## Non-Goals

- Do not change registration state transitions, authorization, confirmation actions, or persistence.
- Do not expose pending registrations through public tournament responses.
- Do not remove or change the meaning of `ActiveTeamRegistration` in this change.
