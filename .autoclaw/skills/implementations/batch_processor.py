"""
Batch Processor Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import json
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Dict, Any, List

def process_batch(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Process a batch of items in parallel"""
    items = input_data.get("items", [])
    batch_id = input_data.get("batch_id")
    workflow = input_data.get("workflow")
    max_workers = input_data.get("max_workers", 10)
    timeout_per_item = input_data.get("timeout_per_item", 60)
    
    if not items or not batch_id or not workflow:
        return {
            "status": "ERROR",
            "error": "Missing required fields: items, batch_id, workflow"
        }
    
    start = datetime.now()
    results = []
    errors = []
    successful = 0
    failed = 0
    
    def process_item(idx, item):
        try:
            return {"index": idx, "status": "SUCCESS", "item": item, "result": {"workflow": workflow}}
        except Exception as e:
            return {"index": idx, "status": "FAILED", "item": item, "error": str(e)}
    
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {executor.submit(process_item, i, item): i for i, item in enumerate(items)}
        for future in as_completed(futures):
            outcome = future.result()
            if outcome["status"] == "SUCCESS":
                successful += 1
                results.append(outcome)
            else:
                failed += 1
                errors.append(outcome)
    
    duration = (datetime.now() - start).total_seconds()
    status = "COMPLETED" if failed == 0 else ("PARTIAL" if successful > 0 else "FAILED")
    
    return {
        "batch_id": batch_id,
        "status": status,
        "total_items": len(items),
        "processed_items": successful + failed,
        "successful": successful,
        "failed": failed,
        "results": results,
        "errors": errors,
        "summary": {
            "workflow": workflow,
            "success_rate": successful / len(items) if items else 0
        },
        "duration": f"{duration:.2f}s"
    }
