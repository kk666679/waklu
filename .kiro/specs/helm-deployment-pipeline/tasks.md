# Implementation Plan: helm-deployment-pipeline

## Overview

Complete the HalalChain deployment infrastructure by extending the skeleton Helm chart to cover all five services, adding blue/green and canary release patterns, wiring staging + smoke-gate + production jobs into the GitHub Actions release workflow, and replacing stub Makefile targets with functional commands. All templates are authored in YAML/Go templates (Helm), CI in GitHub Actions YAML, and operational commands in GNU Make.

---

## Tasks

- [x] 1. Rewrite `values.yaml` with the full per-service structure
  - [x] 1.1 Replace `values.yaml` with the full per-service schema from the design
    - Add per-service blocks for `platformApi`, `web`, `marketplace`, `aiInference`, and `tawheed` — each with `image.repository`, `image.tag` (set to `"dev"`), `replicaCount`, `port`, `autoscaling`, and `ingress.host` fields
    - Add top-level `blueGreen` block (`enabled: false`, `activeSlot: blue`, `previewTag: "dev"`) and `canary` block (`enabled: false`, `replicaCount: 1`, `imageTag: "dev"`)
    - Ensure no service carries `"latest"` as the default tag value
    - _Requirements: 7.2_

- [x] 2. Create `_helpers.tpl` with validation and standard named templates
  - [x] 2.1 Create `deploy/helm/halalchain/templates/_helpers.tpl`
    - Define `halalchain.chart`, `halalchain.labels`, `halalchain.selectorLabels` named templates
    - Define `halalchain.validateImageTag` that calls `fail` when the arg equals `"latest"`
    - _Requirements: 7.4, 1.5_
  - [ ]* 2.2 Write a shell-based property test for Property 10 (`"latest"` tag always fails)
    - **Property 10: "latest" tag always causes template rendering to fail**
    - Run `helm template` with `--set platformApi.image.tag=latest`; assert exit code is non-zero and stderr contains the helpers error message
    - **Validates: Requirements 7.4**

- [x] 3. Rewrite `deployment.yaml` to cover all five services with probes and resource limits
  - [x] 3.1 Rewrite `deploy/helm/halalchain/templates/deployment.yaml` for all five services
    - Emit one `Deployment` per service (platform-api, web/halalchain, marketplace, ai-inference, tawheed); each rendered only when `blueGreen.enabled` is false (or the service is not a blue/green service)
    - Call `halalchain.validateImageTag` before each image reference
    - Set `readinessProbe` and `livenessProbe` per the paths/ports in the design: `/health/ready` + `/health/live` for .NET services, `/health` for tawheed
    - Source image references, ports, env, and resources entirely from `values.yaml`
    - _Requirements: 1.1, 1.2, 1.6, 1.7_
  - [ ]* 3.2 Write a shell-based property test for Property 1 (all five Deployments render)
    - **Property 1: All five services render a Deployment for any valid values**
    - Run `helm template` with all tags set to a valid SHA; count `kind: Deployment` occurrences and assert exactly 5
    - **Validates: Requirements 1.1**
  - [ ]* 3.3 Write a shell-based property test for Property 2 (image references match values)
    - **Property 2: Image references always reflect values.yaml inputs**
    - Run `helm template` with distinct per-service repository+tag pairs; assert each rendered `image:` field matches `<repository>:<tag>` verbatim
    - **Validates: Requirements 1.7, 7.2**

- [x] 4. Create `service.yaml` for all five services with slot-aware selectors
  - [x] 4.1 Create `deploy/helm/halalchain/templates/service.yaml`
    - Render a `ClusterIP` `Service` for each of the five services
    - When `blueGreen.enabled=true` and the service is `halalchain` or `marketplace`, include `slot: {{ .Values.blueGreen.activeSlot }}` in the selector; otherwise use only `app: <service>`
    - _Requirements: 1.2, 2.2, 2.3, 2.6_
  - [ ]* 4.2 Write shell-based property tests for Properties 6 (service selector targets active slot)
    - **Property 6: Service selector always targets the declared active slot**
    - Run `helm template` twice (once with `activeSlot=blue`, once with `activeSlot=green`); assert the Service selector for halalchain and marketplace contains `slot: blue` / `slot: green` respectively, and does not contain the other slot
    - **Validates: Requirements 2.2, 2.6**

- [x] 5. Create `ingress.yaml` conditional on per-service host
  - [x] 5.1 Create `deploy/helm/halalchain/templates/ingress.yaml`
    - Render an `Ingress` resource for each service where `ingress.enabled=true` and `<service>.ingress.host` is a non-empty string
    - Source `ingressClassName` from `ingress.className`; apply any `ingress.annotations`
    - _Requirements: 1.3_
  - [ ]* 5.2 Write a shell-based property test for Property 3 (Ingress count matches configured hosts)
    - **Property 3: Ingress resource count matches configured hosts**
    - Run `helm template` with `ingress.enabled=true` and two out of five hosts set; assert exactly 2 `kind: Ingress` resources are rendered
    - **Validates: Requirements 1.3**

- [x] 6. Create `hpa.yaml` conditional on per-service autoscaling
  - [x] 6.1 Create `deploy/helm/halalchain/templates/hpa.yaml`
    - Render an `HPA` for each service where `<service>.autoscaling.enabled=true`; set `minReplicas`, `maxReplicas`, and `targetCPUUtilizationPercentage` from the corresponding `values.yaml` fields
    - _Requirements: 1.4_
  - [ ]* 6.2 Write a shell-based property test for Property 4 (HPA fields reflect values)
    - **Property 4: HPA fields reflect values.yaml autoscaling config**
    - Run `helm template` enabling autoscaling for one service with explicit min/max/target values; assert the rendered HPA fields match the supplied values
    - **Validates: Requirements 1.4**

- [x] 7. Checkpoint — Ensure `helm lint` passes before release-pattern templates
  - Run `helm lint ./deploy/helm/halalchain` and confirm zero errors and zero warnings before proceeding.
  - Ask the user if any questions arise.

- [x] 8. Create `deployment-bluegreen.yaml` for blue/green services
  - [x] 8.1 Create `deploy/helm/halalchain/templates/deployment-bluegreen.yaml`
    - Render two `Deployment` resources for `halalchain` and `marketplace` (four total) only when `blueGreen.enabled=true`
    - Active-slot Deployment uses `<service>.image.tag`; preview-slot uses `blueGreen.previewTag`
    - Each Deployment pod template carries `slot: blue` or `slot: green` label
    - Call `halalchain.validateImageTag` for both tags
    - _Requirements: 2.1, 2.4, 2.5_
  - [ ]* 8.2 Write shell-based property tests for Properties 5 and 7 (blue/green Deployments)
    - **Property 5: Blue/green produces two labeled Deployments per stateful service**
    - **Property 7: Active and preview slots use distinct image tags**
    - Run `helm template` with `blueGreen.enabled=true`, distinct active and preview tags; assert 2 Deployments per service with correct `slot` labels and correct image tags per slot
    - **Validates: Requirements 2.1, 2.5**

- [x] 9. Create `deployment-canary.yaml` for platform-api canary
  - [x] 9.1 Create `deploy/helm/halalchain/templates/deployment-canary.yaml`
    - Render a `platform-api-canary` `Deployment` only when `canary.enabled=true`
    - Set `spec.replicas` from `canary.replicaCount`; image tag from `canary.imageTag`
    - Pod template labels must include `app: platform-api` and `track: canary`; stable Deployment must include `track: stable`
    - Call `halalchain.validateImageTag` on `canary.imageTag`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_
  - [ ]* 9.2 Write shell-based property tests for Properties 8 and 9 (canary Deployment)
    - **Property 8: Canary Deployment uses canary.imageTag and correct replica count**
    - **Property 9: Both stable and canary Deployments share the app: platform-api label**
    - Run `helm template` with `canary.enabled=true`; assert canary Deployment has correct replicas, image tag, and both Deployments carry `app: platform-api`
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4**

- [x] 10. Checkpoint — Full `helm lint` and `helm template` sanity checks
  - Run `helm lint ./deploy/helm/halalchain` and `helm template` with both `blueGreen.enabled=true` and `canary.enabled=true` to confirm zero errors and correct resource counts before touching CI.
  - Ask the user if any questions arise.

- [x] 11. Add `deploy-staging`, `smoke-gate`, and `deploy-production` jobs to the release workflow
  - [x] 11.1 Add the `deploy-staging` job to `.github/workflows/release.yml`
    - Declare `needs: build-and-publish`; run `helm upgrade --install` to the `halalchain-staging` namespace with `--set platformApi.image.tag=${{ github.sha }}`, `--set web.image.tag=${{ github.sha }}`, `--set marketplace.image.tag=${{ github.sha }}` and `--wait --timeout 5m`
    - _Requirements: 4.1, 4.2, 4.3, 7.1, 7.5_
  - [x] 11.2 Add the `smoke-gate` job to `.github/workflows/release.yml`
    - Declare `needs: deploy-staging`; implement the retry loop (5 attempts, 6 s interval, 30 s `--max-time`) for `/health/ready` on `platform-api` and `/health/live` on `halalchain` and `marketplace`
    - Exit non-zero if any endpoint fails all retries
    - _Requirements: 4.4, 4.5, 4.6, 4.7_
  - [x] 11.3 Add the `deploy-production` job to `.github/workflows/release.yml`
    - Declare `needs: smoke-gate` and `environment: production` for manual approval gating
    - Run `helm upgrade --install` to `halalchain-production` with the same SHA tag; append Helm revision, image SHA, and UTC timestamp to `$GITHUB_STEP_SUMMARY`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 7.5_
  - [ ]* 11.4 Write shell-based property tests for Properties 11, 12, and 16 (smoke-gate and production SHA)
    - **Property 11: Smoke-gate exits non-zero when any endpoint fails after all retries**
    - **Property 12: Smoke-gate passes when all endpoints eventually return 200**
    - **Property 16: Production deploy uses the same image SHA that passed the smoke gate**
    - Extract the `check_endpoint` function into a testable script; run it with a mock server that returns 200 / non-200; assert exit codes and verify `deploy-production` `--set` values reference `github.sha`
    - **Validates: Requirements 4.5, 4.6, 5.2, 7.1, 7.5**

- [x] 12. Replace stub Makefile targets with functional commands
  - [x] 12.1 Rewrite `Makefile` with the five functional targets from the design
    - Implement `deploy`, `rollback`, `smoke-test`, `promote`, and `canary-promote` targets
    - Add the `require` guard macro and apply it to every required variable in every target
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  - [ ]* 12.2 Write shell-based property tests for Properties 13, 14, and 15 (Makefile targets)
    - **Property 13: Makefile targets pass correct values to helm upgrade**
    - **Property 14: Makefile variable guards exit non-zero for any missing required variable**
    - **Property 15: make promote sets blueGreen.activeSlot to any supplied slot value**
    - Use `make --dry-run` or mock `helm` to intercept the helm invocation; assert the command line includes the expected flags and `--set` arguments; invoke each guarded target without its required variable and assert non-zero exit
    - **Validates: Requirements 6.1, 6.4, 6.6**

- [x] 13. Final checkpoint — Ensure all tests pass
  - Run `helm lint ./deploy/helm/halalchain` and all property-test scripts; verify every PASS/FAIL assertion passes and no lint errors remain. Ask the user if any questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for an MVP delivery
- Property-based tests are implemented as shell scripts using `helm template` output assertions; no additional test framework is required
- `values.yaml` placeholder tags (`"dev"`) are intentional — CI overrides them at deploy time with `github.sha`
- The `deploy-production` job requires a GitHub Actions environment named `production` with a protection rule (reviewer approval) configured in the repo settings — this is a GitHub UI action, not a code task
- All `helm upgrade` calls in CI must include `--wait --timeout 5m` to ensure Kubernetes rollout completes before marking the step successful

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1"] },
    { "id": 1, "tasks": ["2.2", "3.1"] },
    { "id": 2, "tasks": ["3.2", "3.3", "4.1"] },
    { "id": 3, "tasks": ["4.2", "5.1", "6.1"] },
    { "id": 4, "tasks": ["5.2", "6.2", "8.1"] },
    { "id": 5, "tasks": ["8.2", "9.1"] },
    { "id": 6, "tasks": ["9.2", "11.1"] },
    { "id": 7, "tasks": ["11.2", "12.1"] },
    { "id": 8, "tasks": ["11.3", "12.2"] },
    { "id": 9, "tasks": ["11.4"] }
  ]
}
```
