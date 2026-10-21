# Containerized verification commands. Source is baked into images; no host SDKs
# or local runtime data/configuration are mounted.
PROD_IMAGE := mix-server
TEST_RUNNER := mix-server-test-runner
DOCKERFILE_TEST := Dockerfile.test

.PHONY: test-unit test-image test-all

# Runs Angular unit tests, then builds the backend and Angular production client
# in an isolated container. MixServer.sln currently contains no backend test
# projects, and the command reports that scope explicitly.
test-unit:
	@echo "=== Building isolated backend and frontend verification image ==="
	docker build -t $(TEST_RUNNER) -f $(DOCKERFILE_TEST) .
	docker run --rm $(TEST_RUNNER)

# Build the complete production image using the repository Dockerfile.
test-image:
	@echo "=== Building production image ==="
	docker build -t $(PROD_IMAGE):test .
	@docker image inspect $(PROD_IMAGE):test --format 'Image: {{.Id}}'

test-all: test-unit test-image
	@echo "=== test-all: backend/frontend build check and image build passed ==="
