SHELL := /bin/bash

.PHONY: deploy rollback smoke-test

deploy:
	@echo "Deploying HalalChain via Helm (dry-run by default)"
	helm template halalchain ./deploy/helm/halalchain >/tmp/halalchain-rendered.yaml
	@echo "Rendered manifest available at /tmp/halalchain-rendered.yaml"

rollback:
	@echo "Rollback command: helm rollback halalchain <revision>"
	@echo "Use the prior known-good release revision."

smoke-test:
	@echo "Running smoke checks against the current deployment"
	@curl -fsS http://localhost:5001/health/ready || echo "platform-api not ready"
	@curl -fsS http://localhost:5200/health/live || echo "halalchain not ready"
	@curl -fsS http://localhost:5201/health/live || echo "marketplace not ready"
