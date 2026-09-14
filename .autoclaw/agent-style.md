# Agent Style Guide — HalalChain

## Overview

This document defines the coding standards and best practices for developing AutoClaw agents within the HalalChain ecosystem.

## Naming Conventions

### Agent Names
- Use lowercase with hyphens: `ingredient-analyzer`
- Descriptive of function: `certificate-validator`
- Versioned if needed: `certificate-validator-v2`

### Configuration Files
- Use snake_case: `halal_e_codes.yaml`
- Group by type: `agents/`, `orchestrator/`, `kg/`

### Input/Output Fields
- Use camelCase for field names
- Use snake_case for file names
- Consistent naming across agents

## Agent Structure

### YAML Configuration

```yaml
# Required fields
name: agent-name
version: 1.0.0
description: Brief description

# Provider configuration
provider:
  type: ollama|openai|anthropic|local
  endpoint: http://localhost:11434
  model: halalchain-assistant
  timeout: 30s

# Parameters
parameters:
  temperature: 0.2
  max_tokens: 2048

# System prompt
system: |
  Multi-line system prompt
  describing agent role and behavior

# Tools
tools:
  - name: tool_name
    description: Tool description
    parameters:
      param1: string

# Knowledge Graph
knowledge:
  - path: .autoclaw/kg/...
    description: Knowledge description

# Memory
memory:
  enabled: true
  ttl: 3600
  max_entries: 1000
```

### System Prompts

System prompts should:

1. **Define Role**: Clearly state agent's purpose
2. **Set Boundaries**: Define what agent can and cannot do
3. **Output Format**: Specify exact output structure
4. **Provide Examples**: Include few-shot examples
5. **Be Concise**: Use clear, direct language

### Output Format

All agents must output structured evidence:

```
[EVIDENCE]
- Key: Value
- Key: Value
- Key: Value

[VERDICT] HALAL|HARAM|MASHBOOH
```

## Error Handling

### Error Categories

| Category | Example | Handling |
|----------|---------|----------|
| Input Validation | Missing required field | Return error with message |
| Network Error | Connection timeout | Retry with backoff |
| Model Error | Invalid response | Log and retry |
| Knowledge Error | Missing knowledge | Fallback to base rules |

### Error Response Format

```yaml
error:
  code: ERROR_CODE
  message: Human-readable message
  details:
    field: Related field
    suggestion: Suggested action
```

## Testing

### Test Structure

```python
# tests/test_agent.py
import pytest
from autoclaw import Agent

class TestHalalAgent:
    def test_halal_case(self):
        """Test HALAL classification."""
        agent = Agent("halal-assistant")
        result = agent.run({
            "ingredients": "wheat flour, sugar, palm oil",
            "certificate": "JAKIM MY12345"
        })
        assert result["verdict"] == "HALAL"
    
    def test_haram_case(self):
        """Test HARAM classification."""
        agent = Agent("halal-assistant")
        result = agent.run({
            "ingredients": "pork lard, salt",
            "certificate": "none"
        })
        assert result["verdict"] == "HARAM"
    
    def test_mashbooh_case(self):
        """Test MASHBOOH classification."""
        agent = Agent("halal-assistant")
        result = agent.run({
            "ingredients": "natural flavors, mono/diglycerides",
            "certificate": "unknown"
        })
        assert result["verdict"] == "MASHBOOH"
```

### Test Coverage

- ✅ Unit tests for each agent
- ✅ Integration tests for orchestrators
- ✅ End-to-end tests for workflows
- ✅ Performance tests for batch processing

## Security

### Data Handling

1. Never log sensitive data (PII, credentials)
2. Always encrypt data at rest
3. Use TLS for all communications
4. Validate all inputs
5. Sanitize all outputs

### Access Control

```yaml
permissions:
  - role: admin
    actions: [create, read, update, delete, configure]
  
  - role: compliance_officer
    actions: [read, verify, audit]
  
  - role: vendor
    actions: [submit, read_own, verify_own]
  
  - role: auditor
    actions: [read, audit, report]
```

## Documentation

### Agent Documentation Template

```markdown
# Agent: [Agent Name]

## Purpose
Brief description of agent's purpose.

## Configuration
Configuration options and parameters.

## Input
Input format and required fields.

## Output
Output format and fields.

## Examples
Example usage and outputs.

## Dependencies
Knowledge graph dependencies, external services.

## Security
Security considerations and requirements.

## Version History
Version history and changes.
```

## Versioning

### Semantic Versioning

- **Major**: Breaking changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, security patches

### Version Format

```
agent-name-vX.Y.Z
```

### Example

```
halal-assistant-v1.2.3
- v1: Major version (breaking changes)
- v2: Minor version (new features)
- 3: Patch version (bug fixes)
```

## Code Review Checklist

- [ ] Follows naming conventions
- [ ] Has complete documentation
- [ ] All tests passing
- [ ] Security review completed
- [ ] Performance impact assessed
- [ ] Version bump done
- [ ] Changelog updated
