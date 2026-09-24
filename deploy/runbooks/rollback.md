# Rollback runbook

## Trigger conditions

- Health checks fail for two consecutive intervals.
- Application errors exceed the agreed SLO threshold.
- Deployment introduces a schema or API contract regression.
- Security or compliance issue requires immediate rollback.

## Immediate response

1. Confirm current release version and last known good version.
2. Pause automated release promotion for the environment.
3. Set the deployment to the previous stable revision.
4. Verify the service returns to readiness and normal traffic levels.
5. Preserve logs, deployment metadata, and rollback diff for the post-incident review.

## Kubernetes rollback

```bash
helm rollback halalchain <previous_revision>
```

## Validation steps

```bash
kubectl get deploy,pods,svc -l app=halalchain
kubectl logs deployment/halalchain-platform-api --tail=200
curl -fsS http://localhost:5001/health/ready
```

## Post-rollback checklist

- Confirm no data migration is pending.
- Verify the outbox queue has drained or recovered.
- Re-enable automated promotion only after a manual signoff.
- Document the incident and the release rollback reason.
