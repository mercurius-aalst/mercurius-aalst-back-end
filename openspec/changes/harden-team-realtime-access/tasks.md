## 1. Authentication And Hub Mapping

- [x] 1.1 Reuse one canonical team hub route for exact-path single-value JWT query-token handling while preserving prior token and Authorization-header behavior.
- [x] 1.2 Configure the mapped hub to close connections on authentication expiration without changing its route or authorization boundary.

## 2. Process-Local Connection Access

- [x] 2.1 Add the domain-neutral Platform connection manager contract and cohesive singleton SignalR implementation with multiple-connection/group tracking and one async gate.
- [x] 2.2 Update the team hub to register personal groups, revalidate serialized team joins, track explicit leaves, and clean tracking on disconnect.
- [x] 2.3 Add post-commit Teams and Identity revocation calls for member removal, leave, team deletion, and first account deletion, with explicit post-commit failure semantics.

## 3. Regression Coverage

- [x] 3.1 Add focused JWT and hub-mapping tests for exact path, query cardinality/value validation, header/prior-state preservation, and authentication-expiration closure.
- [x] 3.2 Add connection-manager and hub lifecycle tests for multiple connections, isolation, leave/disconnect cleanup, and the join-versus-revoke race.
- [x] 3.3 Add Teams and Identity tests proving post-commit ordering, no revocation on failed transactions, and committed state when post-commit revocation fails.

## 4. Validation

- [x] 4.1 Run focused and full restore/build/test/format validation, strict OpenSpec validation, and the EF Core pending-model check.
- [x] 4.2 Verify implementation against the OpenSpec artifacts and audit the final diff/status for scoped, single-process behavior.
