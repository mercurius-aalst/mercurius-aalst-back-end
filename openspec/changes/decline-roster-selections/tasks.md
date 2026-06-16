## 1. API Contract

- [x] 1.1 Add a roster selection action DTO that supports confirm and decline intents.
- [x] 1.2 Replace the confirm-only route with the authenticated `PUT /teams/{teamId:guid}/roster/members/{rosterMemberId:guid}` route.
- [x] 1.3 Update route coverage for the renamed selection endpoint.
- [x] 1.4 Rename related SignalR event, publisher, payload, and state DTO fields to roster selection terminology.

## 2. Service Behavior

- [x] 2.1 Replace confirm-only service entry point with action-based selection handling.
- [x] 2.2 Implement decline by removing the pending team registration and clearing affected pending selections from actionable state.
- [x] 2.3 Automatically decline same-tournament pending selections for the captain before submitting their own team roster.
- [x] 2.4 Preserve confirmation eligibility rechecks and team activation behavior.

## 3. Regression Coverage

- [x] 3.1 Update confirmation tests to use the new action-based service API.
- [x] 3.2 Add tests for selected-member decline and invalid selected-member access.
- [x] 3.3 Add tests for auto-decline when a pending selected user submits their own team roster.
- [x] 3.4 Run `dotnet test LAN.API.sln`.
