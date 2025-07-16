#!/usr/bin/env node

const { execSync } = require('child_process');
const https = require('https');

// Configuration
const ANTHROPIC_API_KEY = process.env.ANTHROPIC_API_KEY;
const GITHUB_TOKEN = process.env.GITHUB_TOKEN;
const GITHUB_OUTPUT = process.env.GITHUB_OUTPUT;

// Get the latest tag
function getLatestTag() {
    try {
        const tag = execSync('git describe --tags --abbrev=0', { encoding: 'utf-8' }).trim();
        return tag;
    } catch (error) {
        console.log('No tags found, starting from v0.0.0');
        return 'v0.0.0';
    }
}

// Get commits since last tag
function getCommitsSince(tag) {
    try {
        const commits = execSync(
            tag === 'v0.0.0' 
                ? 'git log --pretty=format:"%h|%s|%an|%ae" --reverse'
                : `git log ${tag}..HEAD --pretty=format:"%h|%s|%an|%ae" --reverse`,
            { encoding: 'utf-8' }
        ).trim().split('\n').filter(Boolean);
        
        return commits.map(commit => {
            const [hash, message, author, email] = commit.split('|');
            return { hash, message, author, email };
        });
    } catch (error) {
        return [];
    }
}

// Get changed files
function getChangedFiles(tag) {
    try {
        const files = execSync(
            tag === 'v0.0.0'
                ? 'git diff --name-only $(git rev-list --max-parents=0 HEAD)..HEAD'
                : `git diff --name-only ${tag}..HEAD`,
            { encoding: 'utf-8' }
        ).trim().split('\n').filter(Boolean);
        
        return files;
    } catch (error) {
        return [];
    }
}

// Parse version string
function parseVersion(version) {
    const match = version.match(/v?(\d+)\.(\d+)\.(\d+)/);
    if (match) {
        return {
            major: parseInt(match[1]),
            minor: parseInt(match[2]),
            patch: parseInt(match[3])
        };
    }
    return { major: 0, minor: 0, patch: 0 };
}

// Make API request to Claude
async function analyzeWithClaude(commits, files, currentVersion) {
    const prompt = `You are a semantic versioning expert for a .NET project called ViceMCP, which provides MCP (Model Context Protocol) tools for interfacing with the VICE Commodore emulator.

Current version: ${currentVersion}

Recent commits:
${commits.map(c => `- ${c.message}`).join('\n')}

Changed files:
${files.join('\n')}

Based on semantic versioning rules:
- MAJOR: Breaking API changes
- MINOR: New features, backwards compatible
- PATCH: Bug fixes, backwards compatible

Analyze these changes and respond with a JSON object:
{
  "recommendation": "major" | "minor" | "patch" | "none",
  "shouldRelease": true | false,
  "reason": "brief explanation"
}

Consider:
- Changes to public APIs or tool interfaces are breaking
- New MCP tools or commands are features
- Test additions/changes alone don't warrant releases
- Documentation changes alone don't warrant releases
- CI/CD changes alone don't warrant releases`;

    return new Promise((resolve, reject) => {
        const data = JSON.stringify({
            model: 'claude-3-haiku-20240307',
            max_tokens: 500,
            messages: [{
                role: 'user',
                content: prompt
            }]
        });

        const options = {
            hostname: 'api.anthropic.com',
            path: '/v1/messages',
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'x-api-key': ANTHROPIC_API_KEY,
                'anthropic-version': '2023-06-01'
            }
        };

        const req = https.request(options, (res) => {
            let body = '';
            res.on('data', (chunk) => body += chunk);
            res.on('end', () => {
                try {
                    const response = JSON.parse(body);
                    if (response.content && response.content[0]) {
                        const text = response.content[0].text;
                        const jsonMatch = text.match(/\{[\s\S]*\}/);
                        if (jsonMatch) {
                            resolve(JSON.parse(jsonMatch[0]));
                        } else {
                            reject(new Error('No JSON found in response'));
                        }
                    } else {
                        reject(new Error('Invalid response format'));
                    }
                } catch (error) {
                    reject(error);
                }
            });
        });

        req.on('error', reject);
        req.write(data);
        req.end();
    });
}

// Fallback version determination
function determineFallbackVersion(commits, files) {
    // Check for breaking changes
    if (commits.some(c => c.message.includes('BREAKING') || c.message.includes('!'))) {
        return { recommendation: 'major', shouldRelease: true, reason: 'Breaking changes detected' };
    }
    
    // Check for features
    if (commits.some(c => c.message.startsWith('feat:'))) {
        return { recommendation: 'minor', shouldRelease: true, reason: 'New features added' };
    }
    
    // Check for fixes
    if (commits.some(c => c.message.startsWith('fix:'))) {
        return { recommendation: 'patch', shouldRelease: true, reason: 'Bug fixes' };
    }
    
    // Check if only CI/docs/tests changed
    const significantFiles = files.filter(f => 
        !f.startsWith('.github/') && 
        !f.endsWith('.md') && 
        !f.includes('test') && 
        !f.includes('Test')
    );
    
    if (significantFiles.length === 0) {
        return { recommendation: 'none', shouldRelease: false, reason: 'Only non-code changes' };
    }
    
    // Default to patch if there are code changes
    return { recommendation: 'patch', shouldRelease: true, reason: 'Code changes detected' };
}

// Main execution
async function main() {
    try {
        const latestTag = getLatestTag();
        const commits = getCommitsSince(latestTag);
        const files = getChangedFiles(latestTag);
        
        console.log(`Latest tag: ${latestTag}`);
        console.log(`Commits since last tag: ${commits.length}`);
        console.log(`Files changed: ${files.length}`);
        
        if (commits.length === 0) {
            console.log('No commits since last tag');
            writeOutput('', 'false');
            return;
        }
        
        let analysis;
        
        if (ANTHROPIC_API_KEY) {
            try {
                console.log('Analyzing with Claude...');
                analysis = await analyzeWithClaude(commits, files, latestTag);
            } catch (error) {
                console.log('Claude analysis failed, using fallback:', error.message);
                analysis = determineFallbackVersion(commits, files);
            }
        } else {
            console.log('No Anthropic API key, using fallback analysis');
            analysis = determineFallbackVersion(commits, files);
        }
        
        console.log('Analysis:', analysis);
        
        if (!analysis.shouldRelease) {
            console.log('No release needed');
            writeOutput('', 'false');
            return;
        }
        
        // Calculate new version
        const current = parseVersion(latestTag);
        let newVersion;
        
        switch (analysis.recommendation) {
            case 'major':
                newVersion = `v${current.major + 1}.0.0`;
                break;
            case 'minor':
                newVersion = `v${current.major}.${current.minor + 1}.0`;
                break;
            case 'patch':
                newVersion = `v${current.major}.${current.minor}.${current.patch + 1}`;
                break;
            default:
                console.log('No version bump needed');
                writeOutput('', 'false');
                return;
        }
        
        console.log(`New version: ${newVersion}`);
        writeOutput(newVersion, 'true', analysis.reason);
        
    } catch (error) {
        console.error('Error:', error.message);
        process.exit(1);
    }
}

function writeOutput(version, shouldRelease, reason = '') {
    if (GITHUB_OUTPUT) {
        const output = `new_version=${version}\nshould_release=${shouldRelease}\nreason=${reason}`;
        require('fs').appendFileSync(GITHUB_OUTPUT, output + '\n');
    } else {
        console.log('GitHub Actions outputs:');
        console.log(`new_version=${version}`);
        console.log(`should_release=${shouldRelease}`);
        console.log(`reason=${reason}`);
    }
}

// Run the script
main().catch(console.error);