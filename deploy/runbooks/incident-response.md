# Incident response playbook

## Severity levels

- Sev-1: production outage or critical security issue
- Sev-2: degraded service, customer-facing failure, or elevated error rate
- Sev-3: non-critical operational event

## Response steps

1. Acknowledge the incident and assign an incident commander.
2. Confirm service health and dependency status.
3. Evaluate whether a rollback, traffic drain, or provider failover is needed.
4. Capture the evidence set: logs, metrics, configs, and deployment metadata.
5. Communicate status to stakeholders with timeline updates every 30 minutes.
6. After mitigation, perform a postmortem and update the risk register.

## Typical incident sources

- API health failure
- AI provider latency or outage
- Postgres connectivity issue
- Redis saturation or memory pressure
- Authentication or token validation failure
- Unexpected schema migration or data mismatch

## Immediate escalation

- Platform owner: on-call engineering lead
- Security owner: security review contact
- Data owner: compliance or platform lead
