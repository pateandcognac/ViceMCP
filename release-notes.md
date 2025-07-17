# ViceMCP v0.7.0 Release Notes

This release of ViceMCP introduces several new features and improvements to the .NET MCP tools for the VICE Commodore emulator. The primary focus of this update is on enhancing the overall functionality and usability of the library.

## 🚀 Features

- Implemented support for additional MCP commands, expanding the range of emulator interactions available to developers.
- Added the ability to retrieve detailed information about the currently loaded cartridge, including its type, ID, and other metadata.
- Introduced a new API for seamless integration with the VICE emulator's event system, allowing developers to subscribe to key events and react accordingly.

## 🐛 Bug Fixes

- Resolved an issue that could cause incorrect handling of certain MCP responses, ensuring more reliable and consistent behavior.
- Fixed a bug that prevented the library from correctly parsing some emulator configuration settings.

## 🔍 Improvements

- Optimized the internal logic for processing MCP data, resulting in improved performance and reduced resource consumption.
- Enhanced the error handling mechanisms, providing more detailed and informative error messages to aid in troubleshooting.
- Improved the overall code quality and maintainability through refactoring and increased test coverage.

## ⚠️ Breaking Changes

This release includes a breaking change to the `ViceEmulatorConfiguration` class. The `CartridgeType` property has been renamed to `CartridgeInfo` to better reflect the expanded information it now provides. Developers using the previous version of the library will need to update their code accordingly.

## 📚 Technical Details

- The new `CartridgeInfo` property in the `ViceEmulatorConfiguration` class now returns a `CartridgeInfo` object, which contains detailed information about the currently loaded cartridge, including its type, ID, and other metadata.
- The new event-based API allows developers to subscribe to various emulator events, such as frame updates, key presses, and more, enabling more advanced integration and event-driven programming.
- The internal MCP command processing logic has been optimized to reduce overhead and improve overall performance.

For more information and detailed documentation, please refer to the [ViceMCP GitHub repository](https://github.com/your-organization/vicemcp).