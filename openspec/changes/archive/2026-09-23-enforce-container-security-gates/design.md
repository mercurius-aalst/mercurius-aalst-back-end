# Design

The final Azure Linux runtime stage updates only the vulnerable `pcre2` package through the image's configured vendor repository and removes package-manager metadata before switching to the existing non-root user. This keeps the existing .NET major version, runtime family, and application startup behavior.

The existing CI scan already records the Trivy outcome, publishes feedback, and then explicitly fails for HIGH/CRITICAL findings, so it remains unchanged.

Delivery loads one tagged Buildx result into the local Docker daemon, scans that exact tag, uploads the JSON report even on failure, and pushes the same tag only after a successful scan. No rebuild occurs between scan and push.

Workflow permissions default to read-only repository contents. Only the release calculation job receives repository and pull-request write access. Docker Hub authentication occurs after a successful scan, immediately before the push, so build and scan steps do not receive registry credentials.
