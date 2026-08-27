## 1. Hub Invocation Throttling

- [x] 1.1 Add a TeamManagementHub-local singleton fixed-window hub filter that acquires the shared authenticated-subject permit before covered invocations execute.
- [x] 1.2 Register the filter only for TeamManagementHub without changing its route, authorization, or lifecycle configuration.

## 2. Regression Coverage

- [x] 2.1 Add focused direct hub-filter tests for shared-subject, cross-connection and cross-method exhaustion, independent subjects, rejected-next suppression, and unrelated invocation pass-through.
- [x] 2.2 Run focused and full validation, strict OpenSpec validation, the pending-model check, and a final scoped-worktree audit.
