# Legacy Test Scripts

This directory contains early Python test scripts that were used during the initial development of ViceMCP. They are no longer needed as the project now has comprehensive .NET unit tests in the ViceMCP.Tests project.

These scripts are preserved for historical reference only:

- `diagnose_vice.py` - VICE connection diagnostics
- `parse_error.py` - Error parsing utilities
- `test_direct.py` - Direct VICE protocol testing
- `test_mcp.py` - Early MCP protocol testing
- `test_mcp_stdio.py` - stdio transport testing
- `test_ping.py` - Basic ping testing
- `test_wait.py` - Timing and wait testing
- `test_write.py` - Memory write testing
- `mcp_errors.log` - Log file from early testing

## Current Testing

For current testing, use the .NET test suite:

```bash
dotnet test
```

The test suite provides comprehensive coverage with mocked dependencies and doesn't require a running VICE instance.