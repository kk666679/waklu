import re
from typing import Dict, Optional

ISSUER_MAP = {"JAKIM": "MY", "MUI": "ID", "ESMA": "AE", "MUIS": "SG", "BPJPH": "ID"}


def extract_certificate(text: str) -> Dict[str, Optional[str]]:
    r: Dict[str, Optional[str]] = {}

    # Look for an explicit certificate number pattern like JAKIM-2024-001234
    m = re.search(r"([A-Z]{2,10}[\s.-]*\d{4}[\s.-]*\d{3,8})", text)
    if m:
        r["certificateNumber"] = m.group(1).strip()
    else:
        m = re.search(r"(?:certificate|cert)[^A-Z0-9]*([A-Z0-9][\w-]{5,})", text, re.IGNORECASE)
        if m:
            r["certificateNumber"] = m.group(1).strip()

    m = re.search(r"(?:issued\s+by|certification\s+body|issuer|authority)[\s:]*([A-Za-z][\w\s&.,]+?)(?:\.\s|$|,\s*(?:valid|scope|product|certified))", text, re.IGNORECASE)
    if m:
        r["issuer"] = m.group(1).strip().rstrip('.')

    m = re.search(r"(?:jurisdiction|country|region)[\s:]*([A-Z]{2,3})\b", text)
    if m: r["jurisdiction"] = m.group(1).strip()

    m = re.search(r"(?:issued|issue\s+date|effective)[\s:]*(\d[\d./-]+)", text, re.IGNORECASE)
    if m: r["issueDate"] = m.group(1).strip()

    m = re.search(r"(?:expir|valid\s+until|expiry)[\s:]*(\d[\d./-]+)", text, re.IGNORECASE)
    if m: r["expiryDate"] = m.group(1).strip()

    m = re.search(r"(?:scope|product|covers?)[\s:]*([A-Za-z][\w\s,.-]+?)\.\s*$", text, re.IGNORECASE | re.MULTILINE)
    if not m:
        m = re.search(r"(?:scope|product|covers?)[\s:]*([A-Za-z][\w\s,.-]+)", text, re.IGNORECASE)
    if m: r["scope"] = m.group(1).strip().rstrip('.')

    if "issuer" not in r or "jurisdiction" not in r:
        for name, code in ISSUER_MAP.items():
            if re.search(r"\b" + name + r"\b", text, re.IGNORECASE):
                if "issuer" not in r:
                    r["issuer"] = name
                if "jurisdiction" not in r:
                    r["jurisdiction"] = code
                break
    # Also try to infer jurisdiction from issuer field if already set
    if "jurisdiction" not in r and "issuer" in r:
        issuer_text = r["issuer"].upper()
        for name, code in ISSUER_MAP.items():
            if name in issuer_text:
                r["jurisdiction"] = code
                break

    if "jurisdiction" not in r and "issuer" in r:
        r["jurisdiction"] = ISSUER_MAP.get(r["issuer"], None)

    return r
