# Runbook: SignalR Connection Drops

**Alert:** `Web_SignalRConnectionDrop`
**Severity:** Warning

## Symptoms
- SignalR connection drop rate > 10/s for 5 minutes
- Web UI losing real-time connections

## Triage
1. Check SignalR hub health
2. Check Redis backend (SignalR uses Redis for scale-out)
3. Check for network issues
4. Check recent deploys

## Mitigation
1. **Redis issue?** -> Follow `docs/runbooks/redis-memory.md`
2. **Network issue?** -> Check network policies
3. **Recent deploy?** -> Roll back
4. **Scale Web?** -> Increase replica count

## Postmortem triggers
- Any SignalR drop alert lasting > 10 minutes
- Any customer-visible real-time update failure