# GitHub Workflows Guide

This repository uses a unified CI/CD pipeline based on the LuaKit project's architecture.

## Main Workflow: `ci-cd.yml`

A comprehensive multi-stage pipeline that handles everything from building and testing to automatic releases.

### Pipeline Stages

#### 1. Build & Test
- **Triggers**: Push to main/develop, pull requests
- **Platforms**: Ubuntu, Windows, macOS
- **Actions**:
  - Builds the project with .NET 9.0 (with 8.0 fallback)
  - Runs comprehensive test suite with coverage
  - Uploads test results and coverage reports
  - Integrates with Codecov for coverage tracking

#### 2. Code Quality
- **Purpose**: Ensure code standards and consistency
- **Checks**:
  - Code formatting verification
  - Static code analysis
  - Warning detection
  - Code style enforcement

#### 3. Security Scan
- **Tool**: Trivy vulnerability scanner
- **Scans**: Dependencies and code for security issues
- **Reports**: SARIF format uploaded to GitHub Security tab

#### 4. Docker Validation
- **Purpose**: Ensure Docker images build correctly
- **Runs**: On pushes and non-draft PRs

#### 5. Auto Release
- **Triggers**: Pushes to main branch after successful tests
- **Features**:
  - AI-powered version analysis using Claude Haiku
  - Automatic semantic versioning
  - Intelligent release note generation
  - No release for docs/tests/CI-only changes

#### 6. Build Release Artifacts
- **Triggers**: When auto-release creates a new version
- **Platforms**: Linux (x64/ARM64), Windows (x64), macOS (x64/ARM64)
- **Output**: Self-contained, trimmed single-file executables

#### 7. Docker Release
- **Builds**: Multi-architecture Docker images
- **Registries**: GitHub Container Registry and Docker Hub
- **Architectures**: linux/amd64, linux/arm64

#### 8. Notifications
- **Optional**: Slack webhook integration
- **Reports**: Release status and version information

### Supporting Workflows

#### `pr-validation.yml`
- Validates PR titles follow conventional commits
- Auto-labels PRs by size and type
- Welcomes first-time contributors

#### `dependabot.yml`
- Auto-merges minor/patch dependency updates
- Requires manual review for major updates

## Configuration

### Required Secrets
- `ANTHROPIC_API_KEY` (optional): Enables AI-powered versioning and release notes
- `DOCKER_PASSWORD` (optional): For Docker Hub publishing

### Optional Variables
- `DOCKER_USERNAME`: Docker Hub username
- `SLACK_WEBHOOK_URL`: For release notifications

## Version Management

The pipeline uses conventional commits for automatic versioning:

- `feat:` → Minor version bump (0.1.0 → 0.2.0)
- `fix:` → Patch version bump (0.1.0 → 0.1.1)
- `feat!:` or `BREAKING CHANGE:` → Major version bump (0.1.0 → 1.0.0)
- `docs:`, `chore:`, `ci:`, `test:` → No release unless code changes

### Examples
```bash
# New feature (minor bump)
git commit -m "feat: add new memory search command"

# Bug fix (patch bump)
git commit -m "fix: resolve buffer overflow in memory operations"

# Breaking change (major bump)
git commit -m "feat!: change API response format

BREAKING CHANGE: API responses now use different field names"
```

## Scripts

The workflow uses Node.js scripts in `.github/scripts/`:

### `analyze-version.js`
- Analyzes commits since last tag
- Determines version bump type
- Decides if release is needed
- Falls back to conventional commit parsing if AI unavailable

### `generate-release-notes.js`
- Creates professional release notes
- Groups changes by category
- Includes test results and statistics
- Uses AI for natural language generation

## Manual Workflows

To manually trigger workflows:

1. Go to Actions tab
2. Select the workflow
3. Click "Run workflow"
4. Fill in required parameters

## Skipping CI

Add `[skip ci]` to commit messages to skip the pipeline:
```bash
git commit -m "docs: update README [skip ci]"
```

## Best Practices

1. **Commit Messages**: Follow conventional commits for automatic versioning
2. **PRs**: Use descriptive titles that match commit conventions
3. **Testing**: Ensure tests pass locally before pushing
4. **Dependencies**: Keep dependencies up to date with Dependabot
5. **Security**: Address security alerts promptly

## Troubleshooting

### Pipeline Failures
1. Check the Actions tab for detailed logs
2. Click on failed jobs to see specific errors
3. Most common issues:
   - Test failures
   - Linting errors
   - Missing dependencies

### Release Issues
1. Verify ANTHROPIC_API_KEY is set if using AI features
2. Check commit messages follow conventions
3. Ensure main branch protection rules allow bot commits

### Docker Issues
1. Verify Docker credentials are set correctly
2. Check Dockerfile syntax
3. Ensure base images are available

For more help, check the [GitHub Actions documentation](https://docs.github.com/actions).