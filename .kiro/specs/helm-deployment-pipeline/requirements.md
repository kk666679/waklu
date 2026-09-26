# Requirements Document

## Introduction

The helm-deployment-pipeline feature completes the HalalChain platform's production deployment infrastructure. It extends the skeleton Helm chart to cover all five application services, adds blue/green and canary release patterns (stateful services use blue/green; the stateless `platform-api` uses canary), wires a staging deploy and health-gate smoke check into the existing GitHub Actions release workflow, and replaces stub Makefile targets with functional deploy, rollback, and promote commands. Image tags in `values.yaml` are replaced by SHA-pinned digest references injected by CI.

## Glossary

- **Helm_Chart**: The Helm chart rooted at `deploy/helm/halalchain/` that renders all Kubernetes manifests for the HalalChain platform.
- **Pipeline**: The GitHub Actions workflow defined in `.github/workflows/release.yml`.
- **Blue/Green_Deployment**: A release strategy that maintains two complete environments (blue = active, green = preview); traffic is switched by updating a Kubernetes Service selector.
- **Canary_Deployment**: A release strategy that routes a configurable percentage of traffic to a new revision before full promotion.
- **Active_Slot**: The `blue` or `green` label value currently selected by the Service selector in a blue/green deployment.
- **Preview_Slot**: The slot not currently selected; receives the new release before promotion.
- **Health_Gate**: A pipeline job that queries service health endpoints and fails the workflow if any check does not return HTTP 200 within the defined timeout.
- **Smoke_Check**: A curl-based probe against `/health/ready` (for `platform-api`) and `/health/live` (for `halalchain` and `marketplace`).
- **Staging_Environment**: A Kubernetes namespace or cluster used to validate a release before production promotion.
- **Production_Environment**: The live Kubernetes namespace or cluster serving end users.
- **SHA_Tag**: A Docker image reference that includes the exact commit SHA digest, preventing mutable tag drift.
- **HPA**: Kubernetes HorizontalPodAutoscaler that scales a Deployment based on CPU utilisation.
- **Ingress**: A Kubernetes Ingress resource that exposes a service externally via an ingress controller.
- **platform-api**: The stateless ASP.NET Core REST API; subject to canary release.
- **halalchain**: The Blazor Server customer-facing UI; subject to blue/green release.
- **marketplace**: The Razor Pages + Blazor Server + SignalR vendor UI; subject to blue/green release.
- **ai-inference**: The FastAPI AI gateway; included in the Helm chart as a non-critical service.
- **tawheed**: The FastAPI evidence and Policy Engine service; included in the Helm chart as a non-critical service.

---

## Requirements

### Requirement 1 — Complete Helm templates for all application services

**User Story:** As a platform operator, I want a single Helm chart that renders Deployment, Service, Ingress, and HPA manifests for every application service, so that the entire platform can be deployed and upgraded from one chart without manual per-service YAML authoring.

#### Acceptance Criteria

1. THE Helm_Chart SHALL include a `Deployment` template for each of the five application services: `platform-api`, `halalchain`, `marketplace`, `ai-inference`, and `tawheed`.
2. THE Helm_Chart SHALL include a `Service` template of type `ClusterIP` for each of the five application services.
3. WHERE `ingress.enabled` is `true` in `values.yaml`, THE Helm_Chart SHALL render an `Ingress` resource for each service that has `ingress.host` defined.
4. WHERE `autoscaling.enabled` is `true` for a service in `values.yaml`, THE Helm_Chart SHALL render an `HPA` resource for that service with `minReplicas`, `maxReplicas`, and `targetCPUUtilizationPercentage` sourced from `values.yaml`.
5. WHEN `helm lint ./deploy/helm/halalchain` is executed, THE Helm_Chart SHALL produce zero lint errors and zero lint warnings.
6. THE Helm_Chart SHALL define `readinessProbe` and `livenessProbe` for each service using the health endpoint paths documented in `service-manifest.yaml`.
7. THE Helm_Chart SHALL source all image `repository` and `tag` fields from `values.yaml`; no image reference SHALL be hard-coded in a template file.

---

### Requirement 2 — Blue/green release support for stateful services

**User Story:** As a platform operator, I want the Helm chart to support a blue/green release pattern for `halalchain` and `marketplace`, so that I can deploy a new version alongside the running version and switch traffic atomically without downtime.

#### Acceptance Criteria

1. THE Helm_Chart SHALL render two `Deployment` resources for `halalchain` and for `marketplace` — one labelled `slot: blue` and one labelled `slot: green` — when `blueGreen.enabled` is `true` in `values.yaml`.
2. WHEN `blueGreen.enabled` is `true`, THE Helm_Chart SHALL render each service's `Service` resource with a `selector` that matches only the pods whose `slot` label equals `values.blueGreen.activeSlot`.
3. THE Helm_Chart SHALL accept a `values.blueGreen.activeSlot` field whose valid values are `blue` and `green`.
4. WHEN `blueGreen.enabled` is `false`, THE Helm_Chart SHALL render a single `Deployment` per service with no `slot` label, preserving backwards compatibility.
5. THE Helm_Chart SHALL supply the preview slot's image tag via a distinct `values.yaml` field (`blueGreen.previewTag`) so that blue and active slots may run different image versions simultaneously.
6. WHEN `helm template` is executed with `blueGreen.enabled=true` and `activeSlot=blue`, THE Helm_Chart SHALL produce a Service selector that matches only pods with `slot: blue`.

---

### Requirement 3 — Canary release support for platform-api

**User Story:** As a platform operator, I want the Helm chart to support a canary release pattern for `platform-api`, so that I can route a small percentage of traffic to the new version and expand the rollout incrementally after confirming error rates remain acceptable.

#### Acceptance Criteria

1. WHEN `canary.enabled` is `true` in `values.yaml`, THE Helm_Chart SHALL render a second `Deployment` for `platform-api` named `platform-api-canary` with a replica count derived from `values.canary.replicaCount`.
2. WHEN `canary.enabled` is `true`, THE Helm_Chart SHALL render the canary `Deployment` with the image tag sourced from `values.canary.imageTag`.
3. WHEN `canary.enabled` is `true`, THE Helm_Chart SHALL render both the stable and canary `Deployment` resources with a shared `app: platform-api` label so that the existing `Service` routes traffic to both sets of pods.
4. THE Helm_Chart SHALL accept a `values.canary.replicaCount` integer field that controls the number of canary replicas independently of the stable `replicaCount`.
5. WHEN `canary.enabled` is `false`, THE Helm_Chart SHALL render only the stable `platform-api` Deployment with no canary resources.
6. WHEN `helm lint ./deploy/helm/halalchain` is executed with `canary.enabled=true`, THE Helm_Chart SHALL produce zero lint errors.

---

### Requirement 4 — Staging deploy job in the release pipeline

**User Story:** As a platform engineer, I want the release workflow to automatically deploy each release to a staging environment and run smoke checks before production is touched, so that regressions are caught before they affect end users.

#### Acceptance Criteria

1. WHEN a release tag matching `v*` is pushed, THE Pipeline SHALL execute a `deploy-staging` job after the `build-and-publish` job completes successfully.
2. THE `deploy-staging` job SHALL render the Helm chart using `helm upgrade --install` targeting the staging namespace.
3. THE `deploy-staging` job SHALL pass the image tag equal to the triggering commit SHA (from `github.sha`) to the Helm release for all three .NET service images.
4. WHEN the `deploy-staging` job completes, THE Pipeline SHALL execute a `smoke-gate` job that queries `/health/ready` on `platform-api` and `/health/live` on `halalchain` and `marketplace` within the staging environment.
5. IF any Smoke_Check endpoint does not return HTTP 200 within 30 seconds, THEN THE `smoke-gate` job SHALL exit with a non-zero status code, blocking the production promote job.
6. THE `smoke-gate` job SHALL retry each health endpoint up to 5 times with a 6-second interval before marking the check as failed.
7. THE `smoke-gate` job SHALL verify Postgres connectivity by confirming the `platform-api` readiness probe passes, given that `platform-api` readiness depends on Postgres per `service-manifest.yaml`.

---

### Requirement 5 — Production promote job in the release pipeline

**User Story:** As a platform engineer, I want the release workflow to promote a validated staging release to production only after the smoke gate passes, so that production deployments are gated on verified health.

#### Acceptance Criteria

1. WHEN the `smoke-gate` job exits with status 0, THE Pipeline SHALL execute a `deploy-production` job.
2. THE `deploy-production` job SHALL run `helm upgrade --install` targeting the production namespace using the same image SHA tag that passed the smoke gate.
3. THE `deploy-production` job SHALL require manual approval via a GitHub Actions environment protection rule before executing, so that a human can confirm promotion.
4. IF the `smoke-gate` job fails, THEN THE Pipeline SHALL skip the `deploy-production` job and report a failed workflow status.
5. THE `deploy-production` job SHALL record the deployed Helm revision number and image SHA in a GitHub Actions job summary for audit purposes.

---

### Requirement 6 — Functional Makefile targets

**User Story:** As a platform operator, I want Makefile targets that perform real deploy, rollback, promote, and smoke-test operations so that day-two operations can be executed from a single entry point without memorising Helm commands.

#### Acceptance Criteria

1. WHEN `make deploy` is executed with `NAMESPACE` and `TAG` variables set, THE Makefile SHALL run `helm upgrade --install` against the target namespace using the specified image tag.
2. WHEN `make rollback` is executed with `NAMESPACE` and `REVISION` variables set, THE Makefile SHALL run `helm rollback` to the specified revision in the target namespace.
3. WHEN `make smoke-test` is executed with `BASE_URL` set, THE Makefile SHALL query `/health/ready` on `platform-api` and `/health/live` on `halalchain` and `marketplace` and print PASS or FAIL for each endpoint.
4. WHEN `make promote` is executed with `NAMESPACE`, `ACTIVE_SLOT`, and `TAG` variables set, THE Makefile SHALL update the `blueGreen.activeSlot` value in the target Helm release to switch the Service selector to the specified slot.
5. WHEN `make canary-promote` is executed with `NAMESPACE` and `STABLE_TAG` variables set, THE Makefile SHALL set `canary.enabled=false` and update the stable image tag in the target Helm release, completing the canary promotion.
6. IF a required variable is not set when a Makefile target is invoked, THEN THE Makefile SHALL print an error message naming the missing variable and exit with a non-zero status code.

---

### Requirement 7 — SHA-pinned image tags in CI and values.yaml

**User Story:** As a security-conscious operator, I want all deployed images to be referenced by their exact commit SHA digest rather than mutable tags, so that the running workload is always traceable to a specific, signed build.

#### Acceptance Criteria

1. THE Pipeline SHALL set the image tag for each built image to `${{ github.sha }}` in the Helm values passed to the staging and production deploy jobs.
2. THE `values.yaml` file SHALL use placeholder tag values (e.g. `"dev"`) that are overridden at deploy time by the Pipeline; THE `values.yaml` file SHALL NOT contain `latest` as a tag value for any application service image.
3. WHEN the Pipeline signs images with Cosign, THE Pipeline SHALL record the image digest in the GitHub Actions job summary alongside the image repository and tag.
4. THE Helm_Chart SHALL reject a deploy where any application service image tag is the string `"latest"`, by including a `helm.sh/chart` annotation and a `_helpers.tpl` validation that fails template rendering when the tag equals `"latest"`.
5. THE `deploy-staging` and `deploy-production` jobs SHALL each pass `--set platformApi.image.tag=${{ github.sha }}`, `--set web.image.tag=${{ github.sha }}`, and `--set marketplace.image.tag=${{ github.sha }}` to the `helm upgrade` command.
