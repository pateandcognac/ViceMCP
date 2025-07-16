# ViceMCP - AI-Powered Commodore Development Bridge 🚀

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![MCP](https://img.shields.io/badge/MCP-Protocol-blue)](https://modelcontextprotocol.io/)
[![VICE](https://img.shields.io/badge/VICE-Emulator-orange)](https://vice-emu.sourceforge.io/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

ViceMCP bridges the gap between modern AI assistants and retro computing by exposing the VICE Commodore emulator's powerful debugging capabilities through the Model Context Protocol (MCP). This enables AI assistants like Claude to directly interact with running Commodore 64, VIC-20, PET, and other 8-bit Commodore computer emulations.

## 🎯 What It Does

ViceMCP transforms AI assistants into powerful debugging companions for Commodore development:

- **Direct Memory Access**: Read, write, search, and manipulate memory in real-time
- **CPU Control**: Step through code, set breakpoints, examine registers
- **Program Management**: Load PRG files, save memory snapshots, trace execution
- **System Control**: Start emulators, reset machines, capture screen contents
- **Interactive Debugging**: AI can analyze your 6502 assembly code while it runs

## 🤔 Why ViceMCP?

### The Problem
Developing for vintage Commodore computers requires deep knowledge of 6502 assembly, memory maps, and hardware quirks. Traditional debugging involves manually inspecting memory, setting breakpoints, and interpreting cryptic hex dumps.

### The Solution
ViceMCP enables AI assistants to:
- **Analyze running code** and explain what's happening in plain English
- **Debug crashes** by examining memory and registers at the point of failure
- **Optimize routines** by profiling and suggesting improvements
- **Teach** by providing real-time explanations of code behavior
- **Automate** repetitive debugging tasks

## 🎮 Who Is This For?

- **Retro Game Developers** building new games for Commodore platforms
- **Demoscene Creators** pushing the limits of 8-bit hardware
- **Assembly Learners** wanting AI-guided exploration of 6502 programming
- **Digital Archaeologists** reverse-engineering vintage software
- **Homebrew Enthusiasts** creating new software for classic hardware

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- VICE emulator (x64sc, x128, xvic, xpet, etc.)
- MCP-compatible AI assistant (e.g., Claude Desktop)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/ViceMCP.git
cd ViceMCP
```

2. Build the project:
```bash
dotnet build
```

3. Configure your AI assistant to use ViceMCP:
```json
{
  "mcpServers": {
    "vicemcp": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/ViceMCP/ViceMCP.csproj"],
      "env": {
        "VICE_BIN_PATH": "/usr/local/bin"
      }
    }
  }
}
```

4. Start VICE with binary monitor enabled:
```bash
x64sc -binarymonitor -binarymonitoraddress 127.0.0.1:6502
```

## 🛠️ Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `VICE_BIN_PATH` | Directory containing VICE executables | System PATH |
| `VICE_MONITOR_PORT` | TCP port for binary monitor | 6502 |
| `VICE_STARTUP_TIMEOUT` | Milliseconds to wait for VICE startup | 2000 |

## 📚 Complete Tool Reference

### Memory Operations

#### `read_memory`
Read bytes from memory and display in hex format.
```
Parameters:
- startHex: Start address (e.g., "0x0400" or "0400")
- endHex: End address (e.g., "0x04FF")
Returns: Hex string like "08-05-0C-0C-0F"
```

#### `write_memory`
Write bytes to memory.
```
Parameters:
- startHex: Start address (e.g., "0xC000")
- dataHex: Space-separated hex bytes (e.g., "A9 00 8D 20 D0")
Returns: Confirmation with bytes written
```

#### `copy_memory`
Copy memory from one location to another.
```
Parameters:
- sourceHex: Source start address
- destHex: Destination start address
- length: Number of bytes to copy
Returns: Confirmation of copy operation
```

#### `fill_memory`
Fill memory region with a byte pattern.
```
Parameters:
- startHex: Start address
- endHex: End address
- pattern: Hex bytes to repeat (e.g., "AA 55")
Returns: Confirmation with pattern used
```

#### `search_memory`
Search for byte patterns in memory.
```
Parameters:
- startHex: Search start address
- endHex: Search end address
- pattern: Hex bytes to find (e.g., "A9 00" for LDA #$00)
- maxResults: Maximum matches to return (default: 10)
Returns: List of addresses where pattern found
```

#### `compare_memory`
Compare two memory regions and show differences.
```
Parameters:
- addr1Hex: First region start
- addr2Hex: Second region start
- length: Bytes to compare
Returns: List of differences or "regions identical"
```

#### `load_program`
Load a PRG file into memory.
```
Parameters:
- filePath: Path to PRG file
- addressHex: Override load address (optional)
Returns: Load address and size information
```

#### `save_memory`
Save memory region to file.
```
Parameters:
- startHex: Start address
- endHex: End address
- filePath: Output file path
- asPrg: Save as PRG with header (default: true)
Returns: Confirmation with bytes saved
```

### CPU Control

#### `get_registers`
Get all CPU register values.
```
Returns: List of registers with hex values (A, X, Y, PC, SP, etc.)
```

#### `set_register`
Set a CPU register value.
```
Parameters:
- registerName: Register name (A, X, Y, PC, SP)
- valueHex: New value in hex
Returns: Confirmation of register update
```

#### `step`
Step CPU by one or more instructions.
```
Parameters:
- count: Instructions to step (default: 1)
- stepOver: Step over subroutines (default: false)
Returns: Number of instructions stepped
```

#### `continue_execution`
Resume execution after hitting a breakpoint.
```
Returns: "Execution resumed"
```

### Breakpoint Management

#### `set_checkpoint`
Set a breakpoint/checkpoint.
```
Parameters:
- startHex: Start address
- endHex: End address (optional, same as start if omitted)
- stopWhenHit: Stop execution on hit (default: true)
- enabled: Initially enabled (default: true)
Returns: Checkpoint number and address range
```

#### `list_checkpoints`
List all checkpoints with status.
```
Returns: List showing checkpoint #, address range, enabled/disabled, hit count
```

#### `delete_checkpoint`
Delete a checkpoint.
```
Parameters:
- checkpointNumber: Checkpoint # to delete
Returns: Confirmation of deletion
```

#### `toggle_checkpoint`
Enable or disable a checkpoint.
```
Parameters:
- checkpointNumber: Checkpoint # to toggle
- enabled: true to enable, false to disable
Returns: New checkpoint state
```

### System Control

#### `reset`
Reset the emulated machine.
```
Parameters:
- mode: "soft" or "hard" (default: "soft")
Returns: Reset confirmation
```

#### `get_info`
Get VICE version information.
```
Returns: VICE version and SVN revision
```

#### `ping`
Check if VICE is responding.
```
Returns: "Pong! VICE is responding"
```

#### `get_banks`
List available memory banks.
```
Returns: List of bank numbers and names (RAM, ROM, IO, etc.)
```

#### `get_display`
Capture current display as image data.
```
Parameters:
- useVic: Use VIC (true) or VICII/VDC (false)
Returns: Display dimensions and image data size
```

#### `quit_vice`
Quit the VICE emulator.
```
Returns: Confirmation of quit
```

### Emulator Management

#### `start_vice`
Launch a VICE emulator instance.
```
Parameters:
- emulatorType: x64sc, x128, xvic, xpet, xplus4, xcbm2, xcbm5x0
- arguments: Additional command line arguments
Returns: Process ID and monitor port
```

### Input/Output

#### `send_keys`
Send keyboard input to VICE.
```
Parameters:
- keys: Text to type (use \n for Return, \t for Tab)
Returns: Confirmation of keys sent
```

## 💡 Example Use Cases

### 1. Debugging a Crash
```
AI: Let me examine what caused the crash...
> read_memory 0x0100 0x01FF  // Check stack
> get_registers              // See CPU state
> read_memory 0xC000 0xC020  // Examine code at PC
```

### 2. Finding Code Patterns
```
AI: I'll search for all JSR instructions to $FFD2...
> search_memory 0x0800 0xBFFF "20 D2 FF"
```

### 3. Interactive Development
```
AI: Let me load your program and set a breakpoint...
> load_program "game.prg"
> set_checkpoint 0x0810      // Break at start
> continue_execution
> step 10 true              // Step over subroutines
```

### 4. Memory Analysis
```
AI: I'll check if the sprite data was copied correctly...
> compare_memory 0x2000 0x3000 64
```

## 🏗️ Architecture

ViceMCP is built on:
- **.NET 9.0** with async/await patterns throughout
- **Model Context Protocol** for AI assistant integration
- **vice-bridge-net** library for VICE binary monitor protocol
- **Dependency Injection** for clean service management

## 🧪 Testing

Run the comprehensive test suite:
```bash
dotnet test
```

Tests use mocking to run without requiring VICE, ensuring fast and reliable CI/CD.

## 🤝 Contributing

Contributions are welcome! Please check out our [Contributing Guidelines](CONTRIBUTING.md).

### Ideas for Enhancement
- Disassembly support for code analysis
- Symbolic debugging with label support
- Memory visualization tools
- Performance profiling commands
- State snapshot management

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [VICE Team](https://vice-emu.sourceforge.io/) for the amazing emulator
- [Anthropic](https://www.anthropic.com/) for the Model Context Protocol
- The Commodore community for keeping 8-bit dreams alive

---

**Ready to supercharge your Commodore development with AI?** Get started with ViceMCP today!