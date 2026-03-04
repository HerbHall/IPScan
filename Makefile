.PHONY: build test lint-md ci clean hooks help

## Default target
help: ## Show this help
	@echo "IPScan — available targets:"
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-12s %s\n", $$1, $$2}'

build: ## Build all projects
	dotnet build

test: ## Run tests (scoped to cross-platform projects)
	dotnet test tests/IPScan.Core.Tests/IPScan.Core.Tests.csproj --verbosity normal
	dotnet test tests/IPScan.CLI.Tests/IPScan.CLI.Tests.csproj --verbosity normal

lint-md: ## Lint markdown files
	npx --yes markdownlint-cli2 "readme.md" "CLAUDE.md" "DECISIONS.md" "docs/**/*.md"

ci: build test lint-md ## Run full CI pipeline (build + test + lint)

clean: ## Clean build artifacts
	dotnet clean

hooks: ## Install git hooks
	@echo "Installing pre-push hook..."
	@mkdir -p .git/hooks
	@ln -sf ../../scripts/pre-push .git/hooks/pre-push
	@echo "Done."
