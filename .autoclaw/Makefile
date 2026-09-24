.PHONY: eval eval-agents eval-skills eval-workflows eval-rag eval-policy eval-blockchain up down

eval:
	autoclaw eval --config eval/eval_config.yaml --report eval/reports/latest.json

eval-agents:
	autoclaw eval --suite eval/agents

eval-skills:
	autoclaw eval --suite eval/skills

eval-workflows:
	autoclaw eval --suite eval/workflows

eval-rag:
	autoclaw eval --suite eval/rag

eval-policy:
	autoclaw eval --suite eval/policy

eval-blockchain:
	autoclaw eval --suite eval/blockchain

up:
	docker compose -f deploy/docker-compose.yml up -d

down:
	docker compose -f deploy/docker-compose.yml down
