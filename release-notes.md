# ViceMCP v0.4.0 Release Notes

This release introduces several new features and improvements to the ViceMCP library, a .NET project that provides tools for interfacing with the VICE Commodore emulator using the Model Context Protocol (MCP).

## Features

:sparkles: Added support for reading and writing Commodore disk images (D64 format) directly within the ViceMCP library.
:sparkles: Implemented new methods to control emulator features like tape loading, reset, and power operations.
:sparkles: Expanded the set of available MCP commands and responses, providing more granular control over the emulator.

## Improvements

:zap: Significantly improved the overall performance and responsiveness of the library, reducing latency and improving the user experience.
:truck: Reorganized and cleaned up the codebase, making it easier to maintain and extend in the future.

## Bug Fixes

:bug: Fixed an issue that could cause the emulator to become unresponsive under certain conditions.
:bug: Resolved a problem with incorrect handling of some MCP responses, ensuring more reliable and consistent behavior.

## Breaking Changes

:warning: The `EmulatorConnection` class has been renamed to `ViceConnection` to better reflect its purpose.
:warning: Several method signatures have been updated to provide a more intuitive and consistent API. Existing code may need to be updated accordingly.

## Technical Details

- **Target Framework:** .NET 9.0
- **Notable Dependencies:** VICE Commodore Emulator, MCP protocol specification

Please refer to the project's documentation for more information on using the ViceMCP library and its new features.