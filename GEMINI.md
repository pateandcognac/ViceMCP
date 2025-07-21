# GEMINI.md

This file provides guidance to Gemini when working with code in this repository.

## Project Overview

ViceMCP is a Model Context Protocol (MCP) server that enables developers of Commodore 8-bit software to use LLMs (like Gemini!) to interact with the VICE emulator. It exposes VICE's binary monitor protocol through MCP tools, allowing AI assistants to read and write memory in running Commodore 64, VIC-20, PET, and other 8-bit Commodore computer emulations.

This bridge between modern AI tools and retro computing enables:
- Automated debugging of 6502 assembly code
- AI-assisted development of Commodore software
- Memory analysis and manipulation during emulation
- Integration of LLMs into retro development workflows

## Essential Commands

```bash
# Build the project
dotnet build

# Run the MCP server (VICE must be running on port 6502)
dotnet run --project ViceMCP/ViceMCP.csproj

# Build for release
dotnet build -c Release

# Run with Docker
docker-compose build
docker-compose up
```

## Environment Variables

The following environment variables can be used to configure ViceMCP:

- `VICE_BIN_PATH`: Path to directory containing VICE binaries (x64sc, xvic, etc.)
  - Default: empty (uses system PATH)
  - Example: `/usr/local/bin` or `C:\Program Files\VICE`
  
- `VICE_MONITOR_PORT`: TCP port for VICE binary monitor connection
  - Default: 6502
  - Example: `6510`
  
- `VICE_STARTUP_TIMEOUT`: Milliseconds to wait for VICE to start
  - Default: 2000
  - Example: `5000`

## Architecture

The codebase follows a clean separation of concerns:

- **Program.cs**: Entry point that sets up the MCP server with stdio transport, logging to stderr, and dependency injection for ViceBridge
- **ViceTools.cs**: MCP tool definitions for all VICE commands (memory, registers, checkpoints, execution control, etc.)
- **ViceBridgeServices.cs**: Simple implementations of ViceBridge dependencies (performance profiler and message history)
- Uses **vice-bridge-net** library for VICE binary monitor protocol communication

## Key Technical Details

- **Framework**: .NET 9.0 with nullable reference types enabled
- **MCP Library**: ModelContextProtocol 0.3.0-preview.2
- **VICE Protocol**: Version 2 binary monitor protocol ([documentation](https://vice-emu.sourceforge.io/vice_13.html#SEC338))
- **Connection**: TCP to localhost:6502 (configurable via VICE_MONITOR_PORT environment variable)

## Important Implementation Notes

1. **VICE Protocol**: Uses the vice-bridge-net library which implements VICE's binary monitor protocol correctly. The library handles all protocol details including message framing, byte ordering, and error responses.

2. **Dependency Injection**: ViceBridge requires several services which are registered in Program.cs. Simple implementations are provided for IPerformanceProfiler and IMessagesHistory.

3. **Connection Management**: ViceBridge maintains a persistent TCP connection to VICE on port 6502. The connection is established on first use and reused for all subsequent operations.

4. **Memory Operations**: 
   - Addresses are parsed from hex strings (e.g., "0x0400" or "0400")
   - Write operations expect space-separated hex byte values (e.g., "DE AD BE EF")
   - Both operations return formatted results with address and data

5. **Testing**: The ViceMCP.Tests project contains unit tests with mocking for ViceBridge to allow testing without a running VICE instance.

## Development Priorities

When working on this codebase, prioritize:
1. Maintaining the clean separation between MCP tool definitions and VICE protocol (handled by vice-bridge-net)
2. Preserving the async patterns throughout
3. Following the existing hex string conventions for addresses and data
4. Using dependency injection for service registration

## Available MCP Tools

The following MCP tools are implemented:

### Memory Operations
- `read_memory`: Read bytes from memory (addresses in hex)
- `write_memory`: Write bytes to memory (data as space-separated hex)
- `copy_memory`: Copy bytes from one memory location to another
- `fill_memory`: Fill memory region with a byte pattern
- `search_memory`: Search for byte patterns in memory
- `compare_memory`: Compare two memory regions and show differences
- `load_program`: Load a PRG file into memory
- `save_memory`: Save memory region to file (PRG or raw binary)

### Register Operations  
- `get_registers`: Get all CPU register values
- `set_register`: Set a specific register value (A, X, Y, PC, SP)

### Execution Control
- `step`: Step CPU by one or more instructions (with step-over support)
- `continue_execution`: Resume execution after breakpoint
- `reset`: Soft or hard reset the machine

### Checkpoint/Breakpoint Management
- `set_checkpoint`: Set a breakpoint at address range
- `list_checkpoints`: List all checkpoints with status
- `delete_checkpoint`: Remove a checkpoint
- `toggle_checkpoint`: Enable/disable a checkpoint

### System Information
- `get_info`: Get VICE version information
- `ping`: Check if VICE is responding
- `get_banks`: List available memory banks
- `get_display`: Capture current display as image data
- `quit_vice`: Quit the VICE emulator

### Emulator Management
- `start_vice`: Launch a VICE emulator (x64sc, x128, xvic, xpet, etc.)

### Input/Output
- `send_keys`: Send keyboard input to VICE (supports escape sequences like \n for Return)

### Batch Execution
- `execute_batch`: Execute multiple VICE commands in a single operation
  - **IMPORTANT**: Always use batch execution when performing multiple related operations (e.g., setting up screens, creating sprites, initializing memory)
  - Batch execution significantly improves performance by reducing round-trip communication overhead
  - Example: Setting up a sprite display can be 10x faster using batch vs individual commands
  - See `batch_examples/` directory for JSON format examples

## Additional Commands That Could Be Implemented

The following commands would further enhance debugging capabilities:

### Code Analysis
- Disassemble memory region to 6502 assembly
- Trace execution history
- Profile code performance
- Get call stack information

### Advanced Memory Operations  
- Memory checksum/CRC calculation
- Memory watch points
- Memory usage analysis

### State Management
- Save/load emulator snapshots
- Compare snapshots
- Undo/redo functionality

### Peripheral Control
- Attach/detach disk images
- Load cartridge files
- Control datasette
- Joystick simulation

### Debugging Aids
- Symbol table management
- Source-level debugging (if debug info available)
- Memory map visualization
- I/O port monitoring

## Testing

Run tests with:
```bash
dotnet test
```

The test project uses xUnit, Moq, and FluentAssertions. Tests mock the ViceBridge to avoid requiring a running VICE instance.