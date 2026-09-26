{{/*
Expand the name of the chart.
*/}}
{{- define "halalchain.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
*/}}
{{- define "halalchain.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart annotation value: <name>-<version>
*/}}
{{- define "halalchain.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels applied to every resource.
*/}}
{{- define "halalchain.labels" -}}
helm.sh/chart: {{ include "halalchain.chart" . }}
app.kubernetes.io/name: {{ include "halalchain.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels — subset of common labels used in matchLabels / Service selectors.
*/}}
{{- define "halalchain.selectorLabels" -}}
app.kubernetes.io/name: {{ include "halalchain.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Validate that an image tag is not "latest".
Usage: {{ include "halalchain.validateImageTag" .Values.<service>.image.tag }}
Fails template rendering with an actionable error if the tag equals "latest".
*/}}
{{- define "halalchain.validateImageTag" -}}
{{- if eq . "latest" -}}
{{- fail "Image tag 'latest' is not permitted. Pass a SHA-pinned tag via --set <service>.image.tag=<sha>." -}}
{{- end -}}
{{- end -}}
