# GitHub Workflows Guide

This repository uses several GitHub Actions workflows for CI/CD and release automation.

## Workflows Overview

### 1. CI Pipeline (`ci.yml`)
- **Trigger**: Push to main/develop, pull requests
- **Purpose**: Build, test, and validate code across multiple platforms
- **Jobs**:
  - Build & Test (Ubuntu, Windows, macOS)
  - Code Quality checks
  - Security scanning
  - Dependency vulnerability checks
  - Docker build validation
  - Code coverage reporting

### 2. Release Pipeline (`release.yml`)
- **Trigger**: Git tags (v*) or manual dispatch
- **Purpose**: Build and publish release artifacts
- **Outputs**:
  - Self-contained binaries for Linux, Windows, macOS (x64 and ARM64)
  - Docker images published to GitHub Container Registry
  - GitHub Release with artifacts

### 3. Auto Version (`auto-version.yml`)
- **Trigger**: Push to main (excluding docs and config changes)
- **Purpose**: Automatic semantic versioning based on conventional commits
- **Features**:
  - Analyzes commit messages (feat: → minor, fix: → patch, BREAKING: → major)
  - Creates git tags
  - Generates release notes
  - Creates GitHub releases

### 4. Manual Release (`manual-release.yml`)
- **Trigger**: Manual workflow dispatch
- **Purpose**: Create releases with manual version control
- **Options**:
  - Choose version bump type (patch/minor/major)
  - Add custom release notes
  - Updates version in project files

### 5. Advanced Versioning (`versioning.yml`)
- **Trigger**: After successful CI run on main
- **Purpose**: AI-powered version analysis using Claude Haiku
- **Requirements**: `ANTHROPIC_API_KEY` secret
- **Features**:
  - Intelligent commit analysis
  - Detailed release notes generation
  - Automatic version determination

### 6. PR Validation (`pr-validation.yml`)
- **Trigger**: Pull request events
- **Purpose**: Validate PR quality
- **Features**:
  - Semantic PR title enforcement
  - Automatic labeling
  - Size labeling
  - First-time contributor welcome

### 7. Dependabot Integration (`dependabot.yml`)
- **Trigger**: Dependabot pull requests
- **Purpose**: Auto-merge safe dependency updates
- **Features**:
  - Auto-merge minor/patch updates
  - Manual review for major updates

## Setup Requirements

### Required Secrets
- `ANTHROPIC_API_KEY` (optional): For AI-powered versioning
- `DOCKER_USERNAME` (optional): For Docker Hub publishing
- `DOCKER_PASSWORD` (optional): For Docker Hub publishing

### Automatic Secrets
- `GITHUB_TOKEN`: Automatically provided by GitHub Actions

## Version Management

### Conventional Commits
The auto-versioning system follows [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - New features (triggers MINOR bump)
- `fix:` - Bug fixes (triggers PATCH bump)
- `docs:` - Documentation changes (PATCH)
- `chore:`, `ci:`, `test:`, `refactor:` - Maintenance (PATCH)
- `BREAKING CHANGE:` or `!` - Breaking changes (triggers MAJOR bump)

### Examples
```bash
# Minor version bump (0.1.0 → 0.2.0)
git commit -m "feat: add new memory search command"

# Patch version bump (0.1.0 → 0.1.1)
git commit -m "fix: resolve buffer overflow in memory operations"

# Major version bump (0.1.0 → 1.0.0)
git commit -m "feat!: change API response format

BREAKING CHANGE: API responses now use different field names"
```

## Manual Release Process

1. Go to Actions → Manual Release
2. Click "Run workflow"
3. Select version bump type
4. Optionally add release notes
5. Click "Run workflow"

The workflow will:
- Calculate new version
- Update project files
- Create git tag
- Generate release notes
- Create GitHub release
- Trigger artifact builds

## Skipping CI

Add `[skip ci]` to commit messages to skip CI runs:
```bash
git commit -m "docs: update README [skip ci]"
```

## Workflow Permissions

Ensure your repository has the following settings:
- Settings → Actions → General → Workflow permissions
- Select "Read and write permissions"
- Check "Allow GitHub Actions to create and approve pull requests"