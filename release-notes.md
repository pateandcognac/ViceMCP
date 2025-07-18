# ViceMCP v0.8.0 Release Notes

This release of ViceMCP targets the .NET 9.0 framework and introduces several new features and improvements to the library's MCP tooling for interfacing with the VICE Commodore emulator.

## Features 🎨

- Implemented support for reading and writing memory banks in the VICE emulator
- Added new methods to the `ViceMemoryManager` class for querying and manipulating memory regions
- Introduced the `ViceRegisterManager` class to provide access to the emulator's hardware registers

## Improvements 🚀

- Optimized the performance of memory read/write operations for improved responsiveness
- Enhanced the exception handling mechanisms to provide more detailed error reporting
- Updated the project documentation with guides and examples for the new functionality

## Breaking Changes ⚠️

- The `ViceMemoryAccess` class has been deprecated in favor of the new `ViceMemoryManager` and `ViceRegisterManager` classes. Existing code will need to be updated to use the new APIs.

## Technical Details 🔧

- The .NET 9.0 target framework was chosen to take advantage of the latest performance enhancements and language features
- The new memory and register management classes utilize low-level P/Invoke calls to communicate directly with the VICE emulator
- Extensive unit and integration tests have been added to ensure the reliability and correctness of the new functionality

## Feedback and Support 💬

We welcome your feedback and suggestions for improving ViceMCP. If you encounter any issues or have ideas for new features, please don't hesitate to [open a GitHub issue](https://github.com/your-project/issues/new) or reach out to the development team.