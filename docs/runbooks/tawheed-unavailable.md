# Runbook: Tawheed Unavailable

**Alert:** `PlatformApi_DownstreamTawheedErrors`
**Severity:** Critical

## Symptoms
- platform-api -> tawheed 5xx rate > 0.5 rps
- Verifications failing due to tawheed being unreachable

## Triage
1. Check `Tawheed_ServiceDown` alert - is tawheed itself down?
2. Check tawhee health endpoint:
   ```bash
   curl -X POST http://tawheed:8000/health
   ```
3. Check tawhee logs:
   ```bash
   kubectl logs -n prod -l app=tawheed --tail=200
   ```
4. Check network connectivity between platform-api and tawheed

## Mitigation
1. **Tawheed is down** -> Follow `docs/runbooks/service-down.md`
2. **Network issue** -> Check security groups, network policies
3. **Degraded mode** -> Enable fallback verification (if configured)

## Postmortem triggers
- Any tawheed unavailability > 5 minutes
- Any customer-visible verification failure due to tawheed