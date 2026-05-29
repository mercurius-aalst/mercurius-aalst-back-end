## Context

`UserEndpoints` already has admin-only delete routes for `/{id:guid}` and `/{username}/account`. `UserService.DeleteUserAsync` normalizes username and anonymizes the matching user. The missing piece is the shorter front-end contract route.

## Goals / Non-Goals

**Goals:**
- Add `DELETE /v1/lan/users/{username}` for admins.
- Reuse existing username delete/anonymization behavior.
- Preserve `DELETE /v1/lan/users/{id:guid}` route behavior.
- Keep the existing `/account` route for compatibility.

**Non-Goals:**
- Changing anonymization semantics.
- Removing the existing `/account` route in this change.
- Adding normal-user delete access to arbitrary usernames.

## Decisions

- Map the new route on the existing admin group so it inherits admin authorization.
- Keep the `/{id:guid}` constraint for ID deletes and add the username route without a GUID constraint conflict.
- Continue using `UserService.DeleteUserAsync(string username)` for normalized username lookup and anonymization.
- Validate blank or malformed usernames before lookup through existing or shared user validation helpers.
- Return not found for missing usernames without revealing additional account state.

## Risks / Trade-offs

- Minimal APIs can have route ambiguity if constraints are loosened later. Tests should cover GUID-looking usernames and normal GUID delete routes.
- The new route is shorter and may shadow future user subresources if added carelessly; future literal routes should be mapped explicitly.
- Current service validation may need tightening for empty usernames before normalization.
