# HalalChain — AutoClaw Skills

> **PROPRIETARY & CONFIDENTIAL**  
> This directory contains reusable skills for AutoClaw agents.  
> Unauthorized use, disclosure, or distribution is strictly prohibited.

## Overview

Skills are reusable, composable capabilities that can be invoked by agents and orchestrators. Each skill encapsulates a specific functionality with defined inputs, outputs, and execution logic.

## Skill Categories

### Core Skills
| Skill | Description | Used By |
|-------|-------------|---------|
| ingredient-verifier | Verifies ingredient halal status | ingredient-analyzer |
| certificate-validator | Validates halal certificates | certificate-validator |
| supply-chain-analyzer | Analyzes supply chain compliance | supply-chain-tracker |
| compliance-checker | Checks overall compliance | halal-assistant |

### Utility Skills
| Skill | Description | Used By |
|-------|-------------|---------|
| batch-processor | Processes multiple items | batch-orchestrator |
| report-generator | Generates compliance reports | compliance-auditor |
| alert-dispatcher | Dispatches alerts | all agents |
| blockchain-recorder | Records to blockchain | all agents |

### Data Skills
| Skill | Description | Used By |
|-------|-------------|---------|
| e-code-lookup | Looks up E-code classifications | ingredient-analyzer |
| jurisdiction-mapper | Maps jurisdiction requirements | halal-assistant |

## Usage

```bash
# List all available skills
autoclaw skills list

# Invoke a skill directly
autoclaw skill run ingredient-verifier --input '{"ingredient": "gelatin"}'

# Use skill in agent configuration
autoclaw agent configure halal-assistant --skill ingredient-verifier
```

## Skill Structure

```yaml
name: skill-name
version: 1.0.0
description: Skill description
type: async|sync|streaming
timeout: 30s

input:
  schema:
    field1: type
    field2: type

output:
  schema:
    result: type
    status: string

execution:
  provider: python|javascript|shell|http
  source: skill.py
  entrypoint: main

security:
  required_permissions: [permission1, permission2]
  data_classification: level_3
```

## Adding New Skills

1. Create a new YAML file in the `skills/` directory
2. Define the skill schema
3. Implement the execution logic
4. Register with AutoClaw
5. Test the skill

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-08-27 | Initial skills configuration |

© HalalChain 2024-2026. All rights reserved.
