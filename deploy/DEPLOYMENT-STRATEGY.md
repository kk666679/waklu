# Deployment and release strategy

## Chosen target

This repository uses a Kubernetes Helm chart as the deployment target because it is the most portable way to represent a real production deployment pattern without locking the project to a single cloud provider. The chart is intentionally minimal and provider-agnostic while still demonstrating a production rollout model.

## Release model

### Blue/green pattern

- The chart is versioned by Helm release and image tag.
- The previous stable release remains active until the new revision passes readiness and smoke checks.
- Rollback is done by promoting the previous revision and updating the Service selector back to the prior set of pods.

### Canary pattern

- Initial release is performed with a small replica footprint or a canary subset of traffic.
- Once readiness and error rates remain within the agreed thresholds, the rollout expands to the full fleet.
- The release automation should include explicit health gates before full promotion.

## Promotion flow

1. Build and sign images in GHCR.
2. Render the Helm chart and validate with `helm lint`.
3. Deploy the new release in a staging environment for smoke testing.
4. Promote to production using the blue/green or canary pattern.
5. Monitor readiness, latency, and error rate for the release window.
6. Roll back immediately on SLO or security validation breakage.

## Smoke checks

- `/health/ready` on `platform-api`
- `/health/live` on the frontends
- dependency validation for Postgres and Redis connectivity
- no regression in error-rate alert conditions

## Operator notes

- Retain the prior release revision for a rollback window.
- Keep release metadata and image digests alongside the deployment record.
- Run the `Makefile` targets as the human-friendly entry point for deploy and rollback actions.
