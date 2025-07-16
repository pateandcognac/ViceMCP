# Contributing to ViceMCP

First off, thank you for considering contributing to ViceMCP! It's people like you that make ViceMCP such a great tool for the Commodore community.

## Code of Conduct

By participating in this project, you are expected to uphold our Code of Conduct:
- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on what is best for the community
- Show empathy towards other community members

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, please include:

- A clear and descriptive title
- Steps to reproduce the issue
- Expected vs actual behavior
- Your environment details (OS, .NET version, VICE version)
- Any relevant error messages or logs

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- A clear and descriptive title
- A detailed description of the proposed functionality
- Example use cases
- Why this enhancement would be useful

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Follow the coding standards** (see below)
3. **Add tests** for any new functionality
4. **Ensure all tests pass** by running `dotnet test`
5. **Update documentation** as needed
6. **Write a good commit message** using conventional commits format

## Development Setup

1. Install prerequisites:
   - .NET 9.0 SDK
   - VICE emulator
   - Git

2. Clone your fork:
   ```bash
   git clone https://github.com/barryw/ViceMCP.git
   cd ViceMCP
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. Run tests:
   ```bash
   dotnet test
   ```

## Coding Standards

### C# Style Guide

- Use 4 spaces for indentation (no tabs)
- Use `PascalCase` for public members and types
- Use `camelCase` for private fields and local variables
- Prefix private fields with underscore (`_privateField`)
- Use meaningful variable and method names
- Keep methods small and focused
- Use async/await for all I/O operations

### Code Quality

- Write self-documenting code
- Add XML documentation for public APIs
- Avoid magic numbers - use constants
- Handle errors gracefully
- Log important operations
- Keep cyclomatic complexity low

### Testing

- Write unit tests for all new functionality
- Maintain code coverage above 75%
- Use descriptive test names that explain what is being tested
- Follow the Arrange-Act-Assert pattern
- Mock external dependencies

## Commit Message Guidelines

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only changes
- `style`: Code style changes (formatting, etc)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `build`: Build system changes
- `ci`: CI configuration changes
- `chore`: Other changes that don't modify src or test files

Examples:
```
feat(tools): add disassemble command for code analysis
fix(memory): handle buffer overflow in memory operations
docs: update README with new installation instructions
```

## Adding New MCP Tools

When adding new MCP tools:

1. Add the tool method to `ViceTools.cs`
2. Use the `[McpServerTool]` attribute
3. Add proper parameter descriptions
4. Handle errors gracefully
5. Return user-friendly messages
6. Add unit tests for the new tool
7. Update the documentation

Example:
```csharp
[McpServerTool(Name = "new_tool"), Description("Description of what the tool does.")]
public async Task<string> NewTool(
    [Description("Parameter description")] string param)
{
    await EnsureStartedAsync();
    
    try 
    {
        // Implementation
        return "Success message";
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to execute: {ex.Message}");
    }
}
```

## Testing with VICE

To test your changes with VICE:

1. Start VICE with binary monitor:
   ```bash
   x64sc -binarymonitor -binarymonitoraddress 127.0.0.1:6502
   ```

2. Run ViceMCP:
   ```bash
   dotnet run --project ViceMCP/ViceMCP.csproj
   ```

3. Use an MCP client to test your changes

## Documentation

- Update README.md if adding new features
- Update CLAUDE.md for AI-specific guidance
- Add XML documentation to public methods
- Include examples in documentation

## Questions?

Feel free to open an issue for any questions about contributing. We're here to help!

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (MIT).