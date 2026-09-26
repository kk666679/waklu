# Design Document

## helm-deployment-pipeline

---

## Overview

This design extends the existing skeleton Helm chart and GitHub Actions release workflow to deliver a production-grade deployment pipeline for all five HalalChain application services. The work divides into four layers:

1. **Helm chart templates** — complete per-service `Deployment`, `Service`, `Ingress`, and `HPA` resources, plus `_helpers.tpl` validation and blue/green and canary conditional rendering.
2. **GitHub Actions release workflow** — three new pipeline jobs (`deploy-staging`, `smoke-gate`, `deploy-production`) wired after the existing `build-and-publish` job.
3. **Makefile targets** — replace stub commands with functional `helm upgrade`, `helm rollback`, and `curl`-based health check invocations.
4. **SHA-pinned image references** — `values.yaml` carries placeholder `"dev"` tags that CI overrides at deploy time; `_helpers.tpl` rejects any tag equal to `"latest"`.

The design preserves backwards compatibility: all new features (blue/green, canary, ingress, HPA) are guarded by `values.yaml` flags that default to `false`.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    GitHub Actions Pipeline                   │
│                                                             │
│  build-and-publish → deploy-staging → smoke-gate → deploy-production │
│       (existing)       (new)           (new)          (new)  │
└─────────────────────────────────────────────────────────────┘
                              │
                    helm upgrade --install
                              │
┌─────────────────────────────────────────────────────────────┐
│             Helm Chart: deploy/helm/halalchain/              │
│                                                             │
│  _helpers.tpl        Chart.yaml        values.yaml          │
│  templates/                                                  │
│    deployment.yaml          (platform-api, stable)          │
│    deployment-canary.yaml   (platform-api, canary)          │
│    deployment-blugreen.yaml (halalchain, marketplace)       │
│    service.yaml             (all 5 services)                │
│    ingress.yaml             (conditional)                   │
│    hpa.yaml                 (conditional)                   │
└─────────────────────────────────────────────────────────────┘
                              │
┌───────────────┬─────────────┴──────────────┬────────────────┐
│  Staging NS   │                            │ Production NS   │
│               │                            │                 │
│ platform-api  │   smoke-gate curl probes   │ platform-api    │
│ halalchain    │  /health/ready             │ halalchain      │
│ marketplace   │  /health/live              │ marketplace     │
│ ai-inference  │                            │ ai-inference    │
│ tawheed       │                            │ tawheed         │
└───────────────┘                            └─────────────────┘
```

---

## Components

### 1. Helm Chart Structure

The chart root stays at `deploy/helm/halalchain/`. The single `templates/deployment.yaml` is replaced by purpose-scoped files:

| File | Responsibility |
|------|---------------|
| `templates/_helpers.tpl` | Named template definitions and "latest" tag validation |
| `templates/deployment.yaml` | Standard (non-blue/green) Deployment for all 5 services |
| `templates/deployment-bluegreen.yaml` | Blue and green Deployments for `halalchain` and `marketplace` when `blueGreen.enabled=true` |
| `templates/deployment-canary.yaml` | Canary Deployment for `platform-api` when `canary.enabled=true` |
| `templates/service.yaml` | ClusterIP Service for all 5 services; selector is slot-aware for blue/green services |
| `templates/ingress.yaml` | Ingress per service, rendered only when `ingress.enabled=true` and a host is defined |
| `templates/hpa.yaml` | HPA per service, rendered only when `autoscaling.enabled=true` for that service |

#### `_helpers.tpl` — "latest" Tag Validation

A named template `halalchain.validateImageTag` is called from every Deployment template. It uses `fail` to abort rendering when the tag equals `"latest"`:

```yaml
{{- define "halalchain.validateImageTag" -}}
{{- if eq . "latest" -}}
{{- fail "Image tag 'latest' is not permitted. Pass a SHA-pinned tag via --set <service>.image.tag=<sha>." -}}
{{- end -}}
{{- end -}}
```

Each Deployment `image:` field calls the validator before emitting the reference:

```yaml
{{- include "halalchain.validateImageTag" .Values.platformApi.image.tag }}
image: "{{ .Values.platformApi.image.repository }}:{{ .Values.platformApi.image.tag }}"
```

---

### 2. Per-Service Deployment Templates

Each service follows a common pattern. All probe paths are sourced from `service-manifest.yaml`:

| Service | Port | Liveness path | Readiness path |
|---------|------|---------------|----------------|
| `platform-api` | 8080 | `/health/live` | `/health/ready` |
| `halalchain` | 8080 | `/health/live` | `/health/ready` |
| `marketplace` | 8080 | `/health/live` | `/health/ready` |
| `ai-inference` | 7071 | `/health/live` | `/health/ready` |
| `tawheed` | 8000 | `/health` | `/health` |

Standard Deployment skeleton (illustrating `platform-api`; other services follow the same structure):

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ .Release.Name }}-platform-api
  labels:
    app: platform-api
    chart: {{ include "halalchain.chart" . }}
spec:
  replicas: {{ .Values.platformApi.replicaCount | default .Values.replicaCount }}
  selector:
    matchLabels:
      app: platform-api
  template:
    metadata:
      labels:
        app: platform-api
    spec:
      containers:
        - name: platform-api
          {{- include "halalchain.validateImageTag" .Values.platformApi.image.tag }}
          image: "{{ .Values.platformApi.image.repository }}:{{ .Values.platformApi.image.tag }}"
          ports:
            - containerPort: {{ .Values.platformApi.port }}
          readinessProbe:
            httpGet:
              path: /health/ready
              port: {{ .Values.platformApi.port }}
            initialDelaySeconds: 20
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /health/live
              port: {{ .Values.platformApi.port }}
            initialDelaySeconds: 30
            periodSeconds: 20
```

---

### 3. Blue/Green Release Pattern

Blue/green applies to `halalchain` and `marketplace`. The feature is gated by `blueGreen.enabled`.

#### values.yaml additions

```yaml
blueGreen:
  enabled: false
  activeSlot: blue          # blue | green
  previewTag: "dev"         # image tag for the non-active (preview) slot
```

#### Template logic (`deployment-bluegreen.yaml`)

When `blueGreen.enabled=true`, two Deployments are rendered per service:

```yaml
{{- if .Values.blueGreen.enabled }}
{{- range $service := list "halalchain" "marketplace" }}
{{- range $slot := list "blue" "green" }}
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ $.Release.Name }}-{{ $service }}-{{ $slot }}
spec:
  replicas: {{ index $.Values $service "replicaCount" | default $.Values.replicaCount }}
  selector:
    matchLabels:
      app: {{ $service }}
      slot: {{ $slot }}
  template:
    metadata:
      labels:
        app: {{ $service }}
        slot: {{ $slot }}
    spec:
      containers:
        - name: {{ $service }}
          {{- $tag := ternary (index $.Values $service "image" "tag") $.Values.blueGreen.previewTag (eq $slot $.Values.blueGreen.activeSlot) }}
          {{- include "halalchain.validateImageTag" $tag }}
          image: "{{ index $.Values $service "image" "repository" }}:{{ $tag }}"
          ...
---
{{- end }}
{{- end }}
{{- end }}
```

#### Service selector (`service.yaml`, blue/green services)

```yaml
{{- if and .Values.blueGreen.enabled (has $serviceName (list "halalchain" "marketplace")) }}
selector:
  app: {{ $serviceName }}
  slot: {{ .Values.blueGreen.activeSlot }}
{{- else }}
selector:
  app: {{ $serviceName }}
{{- end }}
```

Traffic switch is atomic: updating `blueGreen.activeSlot` via `helm upgrade --set blueGreen.activeSlot=green` repoints the Service selector to the already-warmed preview Deployment.

---

### 4. Canary Release Pattern

Canary applies only to `platform-api`. The stable Deployment is unchanged; a second `platform-api-canary` Deployment is added when `canary.enabled=true`. Both share the `app: platform-api` label, so the existing ClusterIP Service routes to both pod sets. Traffic weight is controlled by replica ratio.

#### values.yaml additions

```yaml
canary:
  enabled: false
  replicaCount: 1
  imageTag: "dev"
```

#### Template logic (`deployment-canary.yaml`)

```yaml
{{- if .Values.canary.enabled }}
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ .Release.Name }}-platform-api-canary
spec:
  replicas: {{ .Values.canary.replicaCount }}
  selector:
    matchLabels:
      app: platform-api
      track: canary
  template:
    metadata:
      labels:
        app: platform-api
        track: canary
    spec:
      containers:
        - name: platform-api
          {{- include "halalchain.validateImageTag" .Values.canary.imageTag }}
          image: "{{ .Values.platformApi.image.repository }}:{{ .Values.canary.imageTag }}"
          ...
{{- end }}
```

The stable Deployment always carries `track: stable`; the Service selector uses only `app: platform-api`, capturing both.

Promotion: `make canary-promote` runs `helm upgrade --set canary.enabled=false --set platformApi.image.tag=<STABLE_TAG>`, collapsing back to a single Deployment at the new image.

---

### 5. GitHub Actions Pipeline Extension

Three new jobs are added after `build-and-publish` in `.github/workflows/release.yml`.

#### Job dependency graph

```
build-and-publish
       │
  deploy-staging
       │
   smoke-gate
       │
  deploy-production  (requires environment: production — manual approval)
```

#### `deploy-staging` job

```yaml
deploy-staging:
  needs: build-and-publish
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: azure/setup-helm@v4
    - name: Deploy to staging
      run: |
        helm upgrade --install halalchain ./deploy/helm/halalchain \
          --namespace halalchain-staging --create-namespace \
          --set platformApi.image.tag=${{ github.sha }} \
          --set web.image.tag=${{ github.sha }} \
          --set marketplace.image.tag=${{ github.sha }} \
          --wait --timeout 5m
```

#### `smoke-gate` job

The smoke-gate script implements the retry loop (5 attempts, 6 s interval) and exits non-zero on any failure:

```yaml
smoke-gate:
  needs: deploy-staging
  runs-on: ubuntu-latest
  steps:
    - name: Health checks
      run: |
        check_endpoint() {
          local url=$1
          local attempts=5
          local interval=6
          for i in $(seq 1 $attempts); do
            if curl -fsS --max-time 30 "$url" > /dev/null 2>&1; then
              echo "PASS: $url"
              return 0
            fi
            echo "Attempt $i/$attempts failed for $url, retrying in ${interval}s..."
            sleep $interval
          done
          echo "FAIL: $url"
          return 1
        }

        STAGING_BASE="http://halalchain-staging.svc.cluster.local"
        check_endpoint "${STAGING_BASE}:5001/health/ready" || exit 1
        check_endpoint "${STAGING_BASE}:5200/health/live" || exit 1
        check_endpoint "${STAGING_BASE}:5201/health/live" || exit 1
```

#### `deploy-production` job

```yaml
deploy-production:
  needs: smoke-gate
  runs-on: ubuntu-latest
  environment: production
  steps:
    - uses: actions/checkout@v4
    - uses: azure/setup-helm@v4
    - name: Deploy to production
      run: |
        helm upgrade --install halalchain ./deploy/helm/halalchain \
          --namespace halalchain-production --create-namespace \
          --set platformApi.image.tag=${{ github.sha }} \
          --set web.image.tag=${{ github.sha }} \
          --set marketplace.image.tag=${{ github.sha }} \
          --wait --timeout 5m
    - name: Record deployment metadata
      run: |
        REVISION=$(helm history halalchain -n halalchain-production --max 1 -o json | jq '.[0].revision')
        echo "## Production Deployment" >> $GITHUB_STEP_SUMMARY
        echo "- **Helm revision:** ${REVISION}" >> $GITHUB_STEP_SUMMARY
        echo "- **Image SHA:** ${{ github.sha }}" >> $GITHUB_STEP_SUMMARY
        echo "- **Deployed at:** $(date -u +%Y-%m-%dT%H:%M:%SZ)" >> $GITHUB_STEP_SUMMARY
```

---

### 6. Functional Makefile Targets

The three stub targets are replaced and three new targets are added. All targets guard required variables with an explicit check that prints the missing variable name and exits non-zero.

```makefile
SHELL := /bin/bash
CHART := ./deploy/helm/halalchain
RELEASE := halalchain

# Guard macro — $(call require,VAR_NAME)
define require
  $(if $(value $1),,$(error Variable '$1' is required but not set))
endef

.PHONY: deploy rollback smoke-test promote canary-promote

deploy:
	$(call require,NAMESPACE)
	$(call require,TAG)
	helm upgrade --install $(RELEASE) $(CHART) \
	  --namespace $(NAMESPACE) --create-namespace \
	  --set platformApi.image.tag=$(TAG) \
	  --set web.image.tag=$(TAG) \
	  --set marketplace.image.tag=$(TAG) \
	  --wait --timeout 5m

rollback:
	$(call require,NAMESPACE)
	$(call require,REVISION)
	helm rollback $(RELEASE) $(REVISION) --namespace $(NAMESPACE) --wait

smoke-test:
	$(call require,BASE_URL)
	@for path in "5001/health/ready" "5200/health/live" "5201/health/live"; do \
	  url="$(BASE_URL):$${path}"; \
	  if curl -fsS --max-time 10 "$$url" > /dev/null 2>&1; then \
	    echo "PASS: $$url"; \
	  else \
	    echo "FAIL: $$url"; \
	  fi; \
	done

promote:
	$(call require,NAMESPACE)
	$(call require,ACTIVE_SLOT)
	$(call require,TAG)
	helm upgrade $(RELEASE) $(CHART) \
	  --namespace $(NAMESPACE) --reuse-values \
	  --set blueGreen.activeSlot=$(ACTIVE_SLOT) \
	  --set blueGreen.enabled=true \
	  --wait

canary-promote:
	$(call require,NAMESPACE)
	$(call require,STABLE_TAG)
	helm upgrade $(RELEASE) $(CHART) \
	  --namespace $(NAMESPACE) --reuse-values \
	  --set canary.enabled=false \
	  --set platformApi.image.tag=$(STABLE_TAG) \
	  --wait
```

---

### 7. Updated `values.yaml` Structure

The full `values.yaml` after this feature lands:

```yaml
replicaCount: 2

image:
  pullPolicy: IfNotPresent

# platform-api (canary release)
platformApi:
  image:
    repository: ghcr.io/supporthalal/halalchain-platform-api
    tag: "dev"
  replicaCount: 2
  port: 8080
  env:
    ASPNETCORE_ENVIRONMENT: Production
    ASPNETCORE_URLS: http://+:8080
  autoscaling:
    enabled: false
    minReplicas: 2
    maxReplicas: 10
    targetCPUUtilizationPercentage: 70
  ingress:
    host: ""

# halalchain web UI (blue/green release)
web:
  image:
    repository: ghcr.io/supporthalal/halalchain-web
    tag: "dev"
  replicaCount: 2
  port: 8080
  autoscaling:
    enabled: false
    minReplicas: 2
    maxReplicas: 6
    targetCPUUtilizationPercentage: 70
  ingress:
    host: ""

# marketplace (blue/green release)
marketplace:
  image:
    repository: ghcr.io/supporthalal/halalchain-marketplace
    tag: "dev"
  replicaCount: 2
  port: 8080
  autoscaling:
    enabled: false
    minReplicas: 2
    maxReplicas: 6
    targetCPUUtilizationPercentage: 70
  ingress:
    host: ""

# ai-inference (standard deploy)
aiInference:
  image:
    repository: ghcr.io/supporthalal/halalchain-ai-inference
    tag: "dev"
  replicaCount: 1
  port: 7071
  autoscaling:
    enabled: false
    minReplicas: 1
    maxReplicas: 4
    targetCPUUtilizationPercentage: 80
  ingress:
    host: ""

# tawheed policy engine (standard deploy)
tawheed:
  image:
    repository: ghcr.io/supporthalal/halalchain-tawheed
    tag: "dev"
  replicaCount: 1
  port: 8000
  autoscaling:
    enabled: false
    minReplicas: 1
    maxReplicas: 4
    targetCPUUtilizationPercentage: 80
  ingress:
    host: ""

# Global ingress
ingress:
  enabled: false
  className: nginx
  annotations: {}

# Blue/green (halalchain + marketplace only)
blueGreen:
  enabled: false
  activeSlot: blue
  previewTag: "dev"

# Canary (platform-api only)
canary:
  enabled: false
  replicaCount: 1
  imageTag: "dev"

# Global resource defaults
resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 250m
    memory: 256Mi
```

---

## Data Models

### Helm Values Schema (key paths)

| Values path | Type | Description |
|-------------|------|-------------|
| `<service>.image.repository` | string | OCI image repository |
| `<service>.image.tag` | string | Image tag; never `"latest"` in production |
| `<service>.replicaCount` | integer | Stable replica count |
| `<service>.port` | integer | Container port |
| `<service>.autoscaling.enabled` | bool | Enable HPA for this service |
| `<service>.autoscaling.minReplicas` | integer | HPA lower bound |
| `<service>.autoscaling.maxReplicas` | integer | HPA upper bound |
| `<service>.autoscaling.targetCPUUtilizationPercentage` | integer | HPA CPU target |
| `<service>.ingress.host` | string | Hostname for Ingress; empty = no Ingress |
| `blueGreen.enabled` | bool | Enable blue/green for halalchain + marketplace |
| `blueGreen.activeSlot` | `"blue"` \| `"green"` | Currently live slot |
| `blueGreen.previewTag` | string | Image tag for the preview slot |
| `canary.enabled` | bool | Enable canary Deployment for platform-api |
| `canary.replicaCount` | integer | Number of canary replicas |
| `canary.imageTag` | string | Image tag for canary Deployment |

### Pipeline Artifact: Job Summary Entry

```
## Production Deployment
- Helm revision: <integer>
- Image SHA: <40-char hex>
- Deployed at: <ISO-8601 UTC>
```

---

## Interfaces

### Helm Template Interface

All templates consume exclusively from `values.yaml`. The `_helpers.tpl` exports:

- `halalchain.chart` — `<name>-<version>` annotation value
- `halalchain.labels` — standard label block
- `halalchain.selectorLabels` — selector subset
- `halalchain.validateImageTag` — fails rendering when arg equals `"latest"`

### Makefile Interface

```
make deploy          NAMESPACE=<ns> TAG=<sha>
make rollback        NAMESPACE=<ns> REVISION=<n>
make smoke-test      BASE_URL=<url>
make promote         NAMESPACE=<ns> ACTIVE_SLOT=<blue|green> TAG=<sha>
make canary-promote  NAMESPACE=<ns> STABLE_TAG=<sha>
```

### GitHub Actions Outputs

`deploy-production` exposes:

- `helm-revision` — Helm release revision integer
- `image-sha` — the `github.sha` value used

These are written to `$GITHUB_STEP_SUMMARY` and optionally passed as step outputs for downstream notification jobs.

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| `"latest"` tag passed at deploy time | `helm template` / `helm upgrade` fails with explicit error message from `_helpers.tpl` |
| Required Makefile variable missing | Make guard prints `Error: Variable '<NAME>' is required but not set` and exits 2 |
| Smoke-gate endpoint returns non-200 after 5 retries | Job step exits non-zero; `deploy-production` is skipped; workflow reports failure |
| `helm upgrade --wait` times out | Helm exits non-zero; pipeline job fails; rollback can be triggered via `make rollback` |
| Blue/green promotion to invalid slot value | Helm renders with an incorrect selector; `helm lint` will not catch this — operator must use `blue` or `green` only (documented in Makefile help) |
| Canary `replicaCount` set to 0 | Canary Deployment renders with 0 replicas — effectively inactive but still present; no error |

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: All five services render a Deployment for any valid values

*For any* valid `values.yaml` where all five services have non-`"latest"` image tags, executing `helm template` must produce exactly one `Deployment` resource per service (five total) when neither `blueGreen.enabled` nor `canary.enabled` is true.

**Validates: Requirements 1.1**

---

### Property 2: Image references always reflect values.yaml inputs

*For any* image `repository` and `tag` pair supplied in `values.yaml` (excluding `"latest"`), the `image:` field of the corresponding rendered `Deployment` must equal `"<repository>:<tag>"` verbatim.

**Validates: Requirements 1.7, 7.2**

---

### Property 3: Ingress resource count matches configured hosts

*For any* `values.yaml` where `ingress.enabled=true`, the number of rendered `Ingress` resources must equal the number of services whose `<service>.ingress.host` field is a non-empty string.

**Validates: Requirements 1.3**

---

### Property 4: HPA fields reflect values.yaml autoscaling config

*For any* service with `autoscaling.enabled=true`, the rendered `HPA` resource's `minReplicas`, `maxReplicas`, and `targetCPUUtilizationPercentage` fields must equal the corresponding `values.yaml` inputs.

**Validates: Requirements 1.4**

---

### Property 5: Blue/green produces two labeled Deployments per stateful service

*For any* `values.yaml` with `blueGreen.enabled=true`, the rendered output for `halalchain` and `marketplace` must each contain exactly two `Deployment` resources: one with `slot: blue` and one with `slot: green`.

**Validates: Requirements 2.1**

---

### Property 6: Service selector always targets the declared active slot

*For any* `activeSlot` value in `{blue, green}` when `blueGreen.enabled=true`, the rendered `Service` selector for `halalchain` and `marketplace` must include `slot: <activeSlot>` and must not include the other slot label.

**Validates: Requirements 2.2, 2.6**

---

### Property 7: Active and preview slots use distinct image tags

*For any* `values.yaml` where `blueGreen.enabled=true` and `<service>.image.tag` differs from `blueGreen.previewTag`, the active-slot `Deployment` must reference `<service>.image.tag` and the preview-slot `Deployment` must reference `blueGreen.previewTag`.

**Validates: Requirements 2.5**

---

### Property 8: Canary Deployment uses canary.imageTag and correct replica count

*For any* `values.yaml` with `canary.enabled=true`, the rendered `platform-api-canary` `Deployment` must have `spec.replicas` equal to `canary.replicaCount` and its container image tag must equal `canary.imageTag`.

**Validates: Requirements 3.1, 3.2, 3.4**

---

### Property 9: Both stable and canary Deployments share the app: platform-api label

*For any* `values.yaml` with `canary.enabled=true`, both the stable `platform-api` and the `platform-api-canary` `Deployment` pod template labels must include `app: platform-api`, ensuring a single `Service` routes traffic to both.

**Validates: Requirements 3.3**

---

### Property 10: "latest" tag always causes template rendering to fail

*For any* `values.yaml` where any application service image tag equals the string `"latest"`, `helm template` must exit with a non-zero status code and emit the `_helpers.tpl` error message.

**Validates: Requirements 7.4**

---

### Property 11: Smoke-gate exits non-zero when any endpoint fails after all retries

*For any* set of endpoint responses where at least one endpoint does not return HTTP 200 across all five retry attempts (with 6-second intervals), the smoke-gate script must exit with a non-zero status code.

**Validates: Requirements 4.5, 4.6**

---

### Property 12: Smoke-gate passes when all endpoints eventually return 200

*For any* endpoint that returns HTTP 200 on attempt N ≤ 5, the smoke-gate script must not report that endpoint as failed, and if all endpoints succeed within 5 attempts the script must exit 0.

**Validates: Requirements 4.6**

---

### Property 13: Makefile targets pass correct values to helm upgrade

*For any* `NAMESPACE` and `TAG` values supplied to `make deploy`, the resulting `helm upgrade --install` invocation must include `--namespace <NAMESPACE>` and must set `platformApi.image.tag`, `web.image.tag`, and `marketplace.image.tag` all equal to `<TAG>`.

**Validates: Requirements 6.1**

---

### Property 14: Makefile variable guards exit non-zero for any missing required variable

*For any* Makefile target invoked without one or more of its required variables, the exit code must be non-zero and the output must name the missing variable.

**Validates: Requirements 6.6**

---

### Property 15: make promote sets blueGreen.activeSlot to any supplied slot value

*For any* `ACTIVE_SLOT` value in `{blue, green}`, the `helm upgrade` command issued by `make promote` must include `--set blueGreen.activeSlot=<ACTIVE_SLOT>`.

**Validates: Requirements 6.4**

---

### Property 16: Production deploy uses the same image SHA that passed the smoke gate

*For any* commit SHA that successfully passes the smoke gate in staging, the `deploy-production` job must pass that identical SHA string as the image tag to all three .NET service `--set` arguments in `helm upgrade`.

**Validates: Requirements 5.2, 7.1, 7.5**
