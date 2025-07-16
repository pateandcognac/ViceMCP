# ViceMCP v0.5.0 Release Notes

This release introduces a number of new features and improvements to the ViceMCP .NET library, which provides tools for interfacing with the VICE Commodore emulator. Key highlights include support for additional MCP commands, bug fixes, and performance optimizations.

## 🚀 Features

- 🆕 Added support for the `get_screenshot` MCP command to capture emulator screenshots
- 🆕 Implemented the `set_power_state` MCP command to control the power state of the emulated system
- 🆕 Exposed new `ScreenshotFormat` enum to specify the image format for captured screenshots

## 🐛 Bug Fixes

- 🔧 Resolved an issue where the `get_tape_status` MCP command was not correctly parsing the response
- 🔧 Fixed a bug that could cause the emulator to become unresponsive under certain conditions

## 🧠 Improvements

- ⚡️ Optimized the handling of MCP command responses for improved performance
- 🔍 Enhanced error reporting to provide more detailed information when MCP commands fail

## ⚠️ Breaking Changes

- The `MachineState` enum has been renamed to `PowerState` to better reflect its purpose
- The `get_tape_status` MCP command now returns a more detailed `TapeStatus` object instead of a simple boolean

Please refer to the [ViceMCP documentation](https://github.com/example/vicemcp/docs) for more information on using the new features and handling breaking changes.