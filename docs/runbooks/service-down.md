# Runbook: Service Down

**Alerts:** `<Service>_ServiceDown`, `Tawheed_ServiceDown`, `AiInference_ServiceDown`
**Severity:** Critical

## Symptoms
- Prometheus `up{job="<service>"} == 0` for 1 minute
- Health checks failing
- Load balancer removing pods

## Triage
1. Confirm scope:
   ```bash
   kubectl get pods -n prod -l app=<service>
   kubectl describe pod <pod> -n prod
   kubectl logs <pod> -n prod --tail=200
   ```
2. Check recent deploys / config changes:
   ```bash
   kubectl rollout history deployment/<service> -n prod
   ```
3. Check node health:
   ```bash
   kubectl get nodes
   kubectl top nodes
   ```
4. Check downstream dependencies (if the pod is CrashLooping due to dependency init).

## Mitigation
1. **CrashLoopBackOff with OOMKilled** -> bump memory limit:
   ```bash
   kubectl set resources deployment/<service> -n prod --limits=memory=2Gi
   ```
2. **Image pull failure** -> verify registry access:
   ```bash
   kubectl describe pod <pod> -n prod | grep -A5 Events
   ```
3. **Recent deploy broke it** -> `kubectl rollout undo deployment/<service> -n prod`
4. **Config error** -> check ConfigMap/Secret:
   ```bash
   kubectl get configmap <service>-config -n prod -o yaml
   ```

## Postmortem triggers
- Any service down > 5 minutes
- Any service down twice in 7 days