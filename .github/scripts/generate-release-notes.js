#!/usr/bin/env node

const { execSync } = require('child_process');
const https = require('https');
const fs = require('fs');
const path = require('path');

// Configuration
const ANTHROPIC_API_KEY = process.env.ANTHROPIC_API_KEY;
const GITHUB_TOKEN = process.env.GITHUB_TOKEN;
const GITHUB_OUTPUT = process.env.GITHUB_OUTPUT;

// Get version from command line
const version = process.argv[2];
if (!version) {
    console.error('Usage: node generate-release-notes.js <version>');
    process.exit(1);
}

// Get the previous tag
function getPreviousTag() {
    try {
        const tags = execSync('git tag -l --sort=-version:refname', { encoding: 'utf-8' })
            .trim()
            .split('\n')
            .filter(Boolean);
        
        // Find the tag before the current one
        const currentIndex = tags.indexOf(version);
        if (currentIndex > 0) {
            return tags[currentIndex + 1] || tags[0];
        }
        
        // If current version not found or is the first, return the latest existing tag
        return tags[0] || 'HEAD';
    } catch (error) {
        return 'HEAD';
    }
}

// Get commits between tags
function getCommits(fromTag, toTag) {
    try {
        const range = fromTag === 'HEAD' ? '' : `${fromTag}..${toTag || 'HEAD'}`;
        const commits = execSync(
            `git log ${range} --pretty=format:"%h|%s|%an|%ae|%ad" --date=short`,
            { encoding: 'utf-8' }
        ).trim().split('\n').filter(Boolean);
        
        return commits.map(commit => {
            const [hash, message, author, email, date] = commit.split('|');
            return { hash, message, author, email, date };
        });
    } catch (error) {
        return [];
    }
}

// Get file statistics
function getFileStats(fromTag, toTag) {
    try {
        const range = fromTag === 'HEAD' ? '' : `${fromTag}..${toTag || 'HEAD'}`;
        const stats = execSync(
            `git diff ${range} --shortstat`,
            { encoding: 'utf-8' }
        ).trim();
        
        const filesMatch = stats.match(/(\d+) files? changed/);
        const insertionsMatch = stats.match(/(\d+) insertions?\(\+\)/);
        const deletionsMatch = stats.match(/(\d+) deletions?\(-\)/);
        
        return {
            filesChanged: filesMatch ? parseInt(filesMatch[1]) : 0,
            insertions: insertionsMatch ? parseInt(insertionsMatch[1]) : 0,
            deletions: deletionsMatch ? parseInt(deletionsMatch[1]) : 0
        };
    } catch (error) {
        return { filesChanged: 0, insertions: 0, deletions: 0 };
    }
}

// Run tests and get results
function getTestResults() {
    try {
        console.log('Running tests...');
        const output = execSync('dotnet test --no-build --verbosity quiet', { 
            encoding: 'utf-8',
            stdio: ['ignore', 'pipe', 'pipe']
        });
        
        // Parse test results from output
        const totalMatch = output.match(/Total tests: (\d+)/);
        const passedMatch = output.match(/Passed: (\d+)/);
        const failedMatch = output.match(/Failed: (\d+)/);
        const skippedMatch = output.match(/Skipped: (\d+)/);
        
        return {
            total: totalMatch ? parseInt(totalMatch[1]) : 0,
            passed: passedMatch ? parseInt(passedMatch[1]) : 0,
            failed: failedMatch ? parseInt(failedMatch[1]) : 0,
            skipped: skippedMatch ? parseInt(skippedMatch[1]) : 0
        };
    } catch (error) {
        console.log('Could not run tests:', error.message);
        return null;
    }
}

// Get project info from csproj
function getProjectInfo() {
    try {
        const csprojFiles = execSync('find . -name "ViceMCP.csproj" -type f', { encoding: 'utf-8' })
            .trim()
            .split('\n')
            .filter(Boolean);
        
        if (csprojFiles.length > 0) {
            const content = fs.readFileSync(csprojFiles[0], 'utf-8');
            const targetFrameworkMatch = content.match(/<TargetFramework>([^<]+)<\/TargetFramework>/);
            const versionMatch = content.match(/<Version>([^<]+)<\/Version>/);
            
            return {
                targetFramework: targetFrameworkMatch ? targetFrameworkMatch[1] : 'net9.0',
                version: versionMatch ? versionMatch[1] : version.replace('v', '')
            };
        }
    } catch (error) {
        console.log('Could not read project info:', error.message);
    }
    
    return {
        targetFramework: 'net9.0',
        version: version.replace('v', '')
    };
}

// Make API request to Claude
async function generateWithClaude(commits, stats, testResults, projectInfo) {
    const prompt = `You are a release notes generator for ViceMCP, a .NET project that provides MCP (Model Context Protocol) tools for interfacing with the VICE Commodore emulator.

Version: ${version}
Target Framework: ${projectInfo.targetFramework}

Commits (${commits.length} total):
${commits.slice(0, 20).map(c => `- ${c.message} (${c.hash})`).join('\n')}
${commits.length > 20 ? `... and ${commits.length - 20} more commits` : ''}

Statistics:
- Files changed: ${stats.filesChanged}
- Lines added: ${stats.insertions}
- Lines removed: ${stats.deletions}

${testResults ? `Test Results:
- Total tests: ${testResults.total}
- Passed: ${testResults.passed}
- Failed: ${testResults.failed}
- Skipped: ${testResults.skipped}` : ''}

Generate professional release notes in markdown format that:
1. Start with a brief summary of this release
2. Group changes by category (Features, Bug Fixes, Improvements, etc.)
3. Highlight any breaking changes
4. Include relevant technical details for developers
5. Be concise but informative
6. Use emojis sparingly for section headers only

Focus on user-facing changes and important technical improvements. Don't include routine maintenance unless significant.`;

    return new Promise((resolve, reject) => {
        const data = JSON.stringify({
            model: 'claude-3-haiku-20240307',
            max_tokens: 2000,
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
                        resolve(response.content[0].text);
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

// Generate fallback release notes
function generateFallbackNotes(commits, stats, testResults, projectInfo) {
    let notes = `# Release ${version}\n\n`;
    
    // Summary
    notes += `This release includes ${commits.length} commits with ${stats.filesChanged} files changed, ${stats.insertions} insertions, and ${stats.deletions} deletions.\n\n`;
    
    // Group commits by type
    const features = commits.filter(c => c.message.startsWith('feat:'));
    const fixes = commits.filter(c => c.message.startsWith('fix:'));
    const others = commits.filter(c => !c.message.startsWith('feat:') && !c.message.startsWith('fix:'));
    
    if (features.length > 0) {
        notes += `## 🚀 Features\n\n`;
        features.forEach(c => {
            notes += `- ${c.message.replace(/^feat:\s*/, '')} (${c.hash})\n`;
        });
        notes += '\n';
    }
    
    if (fixes.length > 0) {
        notes += `## 🐛 Bug Fixes\n\n`;
        fixes.forEach(c => {
            notes += `- ${c.message.replace(/^fix:\s*/, '')} (${c.hash})\n`;
        });
        notes += '\n';
    }
    
    if (others.length > 0) {
        notes += `## 📝 Other Changes\n\n`;
        others.slice(0, 10).forEach(c => {
            notes += `- ${c.message} (${c.hash})\n`;
        });
        if (others.length > 10) {
            notes += `- ... and ${others.length - 10} more\n`;
        }
        notes += '\n';
    }
    
    // Test results
    if (testResults && testResults.total > 0) {
        notes += `## ✅ Testing\n\n`;
        notes += `- Total tests: ${testResults.total}\n`;
        notes += `- Passed: ${testResults.passed}\n`;
        if (testResults.failed > 0) notes += `- Failed: ${testResults.failed}\n`;
        if (testResults.skipped > 0) notes += `- Skipped: ${testResults.skipped}\n`;
        notes += '\n';
    }
    
    // Technical details
    notes += `## 📦 Technical Details\n\n`;
    notes += `- Target Framework: ${projectInfo.targetFramework}\n`;
    notes += `- Version: ${projectInfo.version}\n`;
    
    return notes;
}

// Main execution
async function main() {
    try {
        const previousTag = getPreviousTag();
        const commits = getCommits(previousTag, version);
        const stats = getFileStats(previousTag, version);
        const testResults = getTestResults();
        const projectInfo = getProjectInfo();
        
        console.log(`Generating release notes for ${version}`);
        console.log(`Previous tag: ${previousTag}`);
        console.log(`Commits: ${commits.length}`);
        console.log(`File statistics:`, stats);
        
        let releaseNotes;
        
        if (ANTHROPIC_API_KEY) {
            try {
                console.log('Generating release notes with Claude...');
                releaseNotes = await generateWithClaude(commits, stats, testResults, projectInfo);
            } catch (error) {
                console.log('Claude generation failed, using fallback:', error.message);
                releaseNotes = generateFallbackNotes(commits, stats, testResults, projectInfo);
            }
        } else {
            console.log('No Anthropic API key, using fallback generation');
            releaseNotes = generateFallbackNotes(commits, stats, testResults, projectInfo);
        }
        
        // Write to file
        fs.writeFileSync('release-notes.md', releaseNotes);
        console.log('Release notes written to release-notes.md');
        
        // Write to GitHub output
        if (GITHUB_OUTPUT) {
            // Escape for GitHub Actions
            const escaped = releaseNotes
                .replace(/%/g, '%25')
                .replace(/\n/g, '%0A')
                .replace(/\r/g, '%0D');
            fs.appendFileSync(GITHUB_OUTPUT, `release_notes<<EOF\n${releaseNotes}\nEOF\n`);
        }
        
    } catch (error) {
        console.error('Error:', error.message);
        process.exit(1);
    }
}

// Run the script
main().catch(console.error);