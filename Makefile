SHELL := /bin/bash
CHART := ./deploy/helm/halalchain
RELEASE := halalchain

# Guard macro — $(call require,VAR_NAME)
define require
$(if $(value $1),,$(error Variable '$1' is required but not set))
endef

.PHONY: help deploy rollback smoke-test promote canary-promote

help:
	@echo "HalalChain Helm deployment targets"
	@echo ""
	@echo "  make deploy          NAMESPACE=<ns> TAG=<sha>"
	@echo "    Run helm upgrade --install for all five services using the given image tag."
	@echo ""
	@echo "  make rollback        NAMESPACE=<ns> REVISION=<n>"
	@echo "    Roll back the Helm release to the specified revision."
	@echo ""
	@echo "  make smoke-test      BASE_URL=<url>"
	@echo "    Probe /health/ready on platform-api and /health/live on halalchain + marketplace."
	@echo "    Prints PASS or FAIL for each endpoint."
	@echo ""
	@echo "  make promote         NAMESPACE=<ns> ACTIVE_SLOT=<blue|green> TAG=<sha>"
	@echo "    Switch the blue/green Service selector to the specified slot."
	@echo ""
	@echo "  make canary-promote  NAMESPACE=<ns> STABLE_TAG=<sha>"
	@echo "    Disable the canary Deployment and promote its image tag to the stable release."

deploy:
	$(call require,NAMESPACE)
	$(call require,TAG)
	helm upgrade --install $(RELEASE) $(CHART) \
	  --namespace $(NAMESPACE) --create-namespace \
	  --set platformApi.image.tag=$(TAG) \
	  --set web.image.tag=$(TAG) \
	  --set marketplace.image.tag=$(TAG) \
	  --set aiInference.image.tag=$(TAG) \
	  --set tawheed.image.tag=$(TAG) \
	  --wait --timeout 5m

rollback:
	$(call require,NAMESPACE)
	$(call require,REVISION)
	helm rollback $(RELEASE) $(REVISION) --namespace $(NAMESPACE) --wait

smoke-test:
	$(call require,BASE_URL)
	@for path in "5001/health/ready" "5200/health/live" "5201/health/live"; do \
	  url="$(BASE_URL):$${path}"; \
	  if curl -fsS --max-time 10 "$$url" > /dev/null 2>&1; then \
	    echo "PASS: $$url"; \
	  else \
	    echo "FAIL: $$url"; \
	  fi; \
	done

promote:
	$(call require,NAMESPACE)
	$(call require,ACTIVE_SLOT)
	$(call require,TAG)
	helm upgrade $(RELEASE) $(CHART) \
	  --namespace $(NAMESPACE) --reuse-values \
	  --set blueGreen.activeSlot=$(ACTIVE_SLOT) \
	  --set blueGreen.enabled=true \
	  --wait

canary-promote:
	$(call require,NAMESPACE)
	$(call require,STABLE_TAG)
	helm upgrade $(RELEASE) $(CHART) \
	  --namespace $(NAMESPACE) --reuse-values \
	  --set canary.enabled=false \
	  --set platformApi.image.tag=$(STABLE_TAG) \
	  --wait
