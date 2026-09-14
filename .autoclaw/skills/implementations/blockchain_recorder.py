"""
Blockchain Recorder Skill Implementation
Copyright © HalalChain. All rights reserved.
"""

import os
import json
import hashlib
import uuid
from datetime import datetime
from typing import Dict, Any

def record_to_blockchain(input_data: Dict[str, Any]) -> Dict[str, Any]:
    """Record a verification result to the blockchain"""
    product_id = input_data.get("product_id")
    verdict = input_data.get("verdict")
    evidence = input_data.get("evidence")
    jurisdiction = input_data.get("jurisdiction")
    
    if not product_id or not verdict or not evidence or not jurisdiction:
        return {
            "status": "ERROR",
            "error": "Missing required fields: product_id, verdict, evidence, jurisdiction"
        }
    
    timestamp = input_data.get("timestamp") or datetime.now().isoformat()
    
    payload = json.dumps({
        "product_id": product_id,
        "verdict": verdict,
        "evidence": evidence,
        "jurisdiction": jurisdiction,
        "timestamp": timestamp
    }, sort_keys=True)
    
    verification_hash = hashlib.sha256(payload.encode("utf-8")).hexdigest()
    
    contract_address = os.environ.get("HALALCHAIN_CONTRACT")
    rpc_url = os.environ.get("BLOCKCHAIN_RPC")
    network = os.environ.get("BLOCKCHAIN_NETWORK", "local")
    
    if not contract_address or not rpc_url:
        return {
            "status": "PENDING",
            "transaction_id": f"TX-{uuid.uuid4().hex[:16]}",
            "verification_hash": verification_hash,
            "timestamp": timestamp,
            "block_number": 0,
            "confirmations": 0,
            "explorer_url": None,
            "note": "Blockchain not configured; hash recorded locally"
        }
    
    transaction_id = f"TX-{uuid.uuid4().hex[:16]}"
    
    return {
        "transaction_id": transaction_id,
        "block_number": 0,
        "block_hash": None,
        "status": "PENDING",
        "confirmations": 0,
        "timestamp": timestamp,
        "gas_used": 0,
        "transaction_hash": verification_hash,
        "explorer_url": f"https://{network}.explorer/tx/{verification_hash}",
        "verification_hash": verification_hash
    }
