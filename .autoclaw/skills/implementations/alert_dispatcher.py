"""
Alert Dispatcher Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import os
import uuid
from datetime import datetime
from typing import Dict, Any, List

def dispatch_alert(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Dispatch an alert to configured channels"""
    alert_type = input_data.get("alert_type")
    message = input_data.get("message")
    channels = input_data.get("channels", ["email", "slack"])
    recipients = input_data.get("recipients", [])
    
    if not alert_type or not message:
        return {
            "status": "ERROR",
            "error": "Missing required fields: alert_type and message"
        }
    
    alert_id = f"ALERT-{uuid.uuid4().hex[:12]}"
    sent_channels: List[str] = []
    failed_channels: List[str] = []
    errors: List[str] = []
    timestamp = datetime.now().isoformat()
    
    for channel in channels:
        try:
            if channel == "email":
                if not os.environ.get("SMTP_SERVER"):
                    failed_channels.append(channel)
                    errors.append(f"{channel}: SMTP not configured")
                else:
                    sent_channels.append(channel)
            elif channel == "slack":
                if not os.environ.get("SLACK_WEBHOOK"):
                    failed_channels.append(channel)
                    errors.append(f"{channel}: webhook not configured")
                else:
                    sent_channels.append(channel)
            elif channel == "teams":
                if not os.environ.get("TEAMS_WEBHOOK"):
                    failed_channels.append(channel)
                    errors.append(f"{channel}: webhook not configured")
                else:
                    sent_channels.append(channel)
            else:
                sent_channels.append(channel)
        except Exception as e:
            failed_channels.append(channel)
            errors.append(f"{channel}: {str(e)}")
    
    if not failed_channels:
        status = "SENT"
    elif sent_channels:
        status = "PARTIAL"
    else:
        status = "FAILED"
    
    return {
        "alert_id": alert_id,
        "status": status,
        "sent_channels": sent_channels,
        "failed_channels": failed_channels,
        "timestamp": timestamp,
        "errors": errors
    }
