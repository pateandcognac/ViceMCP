#!/bin/bash

# Script to analyze commits and determine version bump using Claude Haiku
# Usage: ./analyze-commits.sh <current_version> <anthropic_api_key>

set -e

CURRENT_VERSION="${1:-v0.0.0}"
ANTHROPIC_API_KEY="${2:-$ANTHROPIC_API_KEY}"

if [ -z "$ANTHROPIC_API_KEY" ]; then
    echo "Error: ANTHROPIC_API_KEY is required"
    exit 1
fi

# Get commits since last tag
LAST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "")
if [ -z "$LAST_TAG" ]; then
    COMMITS=$(git log --pretty=format:"%h %s" --reverse)
else
    COMMITS=$(git log --pretty=format:"%h %s" --reverse ${LAST_TAG}..HEAD)
fi

# Build the prompt
PROMPT=$(cat << EOF
Analyze these git commits and determine the appropriate semantic version bump.
Current version: $CURRENT_VERSION

Commits since last release:
$COMMITS

Rules for version bumping:
- PATCH (0.0.x): Bug fixes, minor updates, documentation, dependency updates
- MINOR (0.x.0): New features, new commands, non-breaking enhancements
- MAJOR (x.0.0): Breaking changes, removal of features, major API changes

Conventional commit analysis:
- feat: → MINOR (new feature)
- fix: → PATCH (bug fix)
- docs: → PATCH (documentation)
- chore:, ci:, test:, refactor:, perf: → PATCH
- BREAKING CHANGE: or ! → MAJOR
- Multiple feat: commits → MINOR (not MAJOR)

Generate comprehensive release notes including:
1. Brief summary (1-2 sentences)
2. Highlights section for major improvements
3. Features section (if any feat: commits)
4. Bug Fixes section (if any fix: commits)
5. Other Changes section for remaining commits
6. Breaking Changes section (if any)

Use bullet points and include commit hashes in parentheses.
Make it engaging and informative for users.

Respond with valid JSON only:
{
  "bump": "patch|minor|major",
  "release_notes": "full markdown release notes",
  "summary": "one line summary"
}
EOF
)

# Call Claude API
RESPONSE=$(curl -s https://api.anthropic.com/v1/messages \
  -H "x-api-key: $ANTHROPIC_API_KEY" \
  -H "anthropic-version: 2023-06-01" \
  -H "content-type: application/json" \
  -d @- << JSON
{
  "model": "claude-3-haiku-20240307",
  "max_tokens": 2000,
  "temperature": 0.3,
  "messages": [{
    "role": "user",
    "content": $(echo "$PROMPT" | jq -Rs .)
  }]
}
JSON
)

# Check for API errors
if echo "$RESPONSE" | jq -e '.error' > /dev/null; then
    echo "Error from Claude API:"
    echo "$RESPONSE" | jq -r '.error.message'
    exit 1
fi

# Extract and output the analysis
echo "$RESPONSE" | jq -r '.content[0].text'