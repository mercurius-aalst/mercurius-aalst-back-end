## 1. Endpoint Mapping

- [x] 1.1 Add a string-bindable enum-backed game action route for lifecycle POST actions.
- [x] 1.2 Remove separate game lifecycle action route mappings.
- [x] 1.3 Add a string-bindable enum-backed current-user account action route for resend verification email and password reset.
- [x] 1.4 Remove separate current-user account action route mappings.
- [x] 1.5 Consolidate current-user received and sent invite route mappings behind `GET /me/invites?sent=<bool>`.
- [x] 1.6 Remove the separate current-user sent invite route mapping.

## 2. Verification

- [x] 2.1 Add or update endpoint route tests for consolidated game lifecycle routes and removed action paths.
- [x] 2.2 Add or update endpoint route tests for consolidated current-user account action routes and removed action paths.
- [x] 2.3 Update team route tests for the consolidated invite route and removed sent-invites path.
- [ ] 2.4 Run the solution tests.
