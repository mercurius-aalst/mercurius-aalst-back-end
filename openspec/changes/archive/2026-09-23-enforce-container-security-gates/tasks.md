## Implementation

- [x] Update the final runtime image to install the vendor-fixed `pcre2` package, remove package-manager metadata, and retain non-root execution.
- [x] Confirm the existing pull-request HIGH/CRITICAL image gate and feedback remain enforced.
- [x] Build the delivery image once, scan the exact tagged artifact, retain its report, and push only after a successful scan.
- [x] Scope write permissions to the release metadata job and defer Docker Hub authentication until the scan passes.

## Validation

- [x] Strictly validate this OpenSpec change before and after implementation.
- [x] Build the final container and verify its installed `pcre2` version and non-root user.
- [x] Scan the fixed final image and confirm no HIGH/CRITICAL findings.
- [x] Scan the original vulnerable runtime image as a negative control and confirm the gate exits nonzero.
- [x] Validate the changed workflow syntax and inspect the build-scan-push dependency order.
- [x] Verify build and scan steps have read-only repository access and no Docker Hub credentials.
