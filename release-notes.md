# ViceMCP v0.3.0 Release Notes

This release of ViceMCP introduces several new features and improvements to the .NET library for interfacing with the VICE Commodore emulator. Notable changes include support for additional emulator commands, performance optimizations, and bug fixes.

## 🚀 Features

- Added support for `get_file_system_info` and `get_file_system_contents` emulator commands to retrieve information about the emulator's virtual file system
- Implemented new `IViceDebugger` interface to provide access to the emulator's debugging capabilities
- Expanded `IViceDrive` interface with methods to control floppy disk drive operations

## 🐛 Bug Fixes

- Fixed an issue where certain emulator commands would fail to parse the response correctly
- Resolved a race condition that could cause data corruption when accessing the emulator's event queue

## 🔍 Improvements

- Optimized memory usage and reduced CPU overhead for frequently used emulator commands
- Improved error handling and exception messages to provide more context for developers

## ⚠️ Breaking Changes

- The `IViceEmulator.SetRam` method has been renamed to `IViceEmulator.SetMemory` to better reflect its purpose
- The signature of the `IViceEventListener.OnEvent` method has been updated to include the event source

## 🧑‍💻 Developer Notes

- The `IViceDebugger` interface provides access to the emulator's debugging capabilities, including the ability to set breakpoints, step through code, and inspect memory
- The `get_file_system_info` and `get_file_system_contents` commands can be used to interact with the emulator's virtual file system, allowing you to list directories, read and write files, and more
- Performance optimizations in this release focused on frequently used emulator commands to ensure smooth and responsive user experiences