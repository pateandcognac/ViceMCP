# ViceMCP v0.8.4 Release Notes

This release of ViceMCP focuses on bug fixes and minor improvements to the library's core functionality. There are no breaking changes in this version.

## Bug Fixes

- Resolved an issue that could cause incorrect emulation state when loading and unloading disk images multiple times.
- Fixed a bug that prevented the `ReadMemory` and `WriteMemory` methods from working correctly on certain memory regions.

## Improvements

- Optimized the performance of disk image handling, resulting in faster load and save times.
- Improved the error messaging for unsupported or corrupted disk images.

## Technical Details

- This release targets the `.NET 9.0` framework.
- No changes were made to the public API, ensuring full backwards compatibility.
- The internal codebase has been refactored to improve maintainability and extensibility.