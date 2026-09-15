# Runbook: Disk Space Low

**Alert:** `DiskSpaceLow`
**Severity:** Warning

## Symptoms
- Disk usage > 85% for 10 minutes
- Disk space < 15% available

## Triage
1. Check disk usage:
   ```bash
   df -h
   ```
2. Check which directories are large:
   ```bash
   du -sh /* 2>/dev/null | sort -rh | head -10
   ```
3. Check for log files
4. Check for Docker volumes

## Mitigation
1. **Clean logs?** -> Rotate/delete old logs
2. **Clean Docker?** -> `docker system prune`
3. **Increase disk?** -> Resize disk
4. **Move data?** -> Move data to another volume

## Postmortem triggers
- Any disk space alert lasting > 30 minutes
- Any service down due to disk space