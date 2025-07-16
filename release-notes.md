# ViceMCP v0.3.2 Release Notes

This release introduces a few bug fixes and minor improvements to the ViceMCP library, which provides a .NET interface for interacting with the VICE Commodore emulator.

## Bug Fixes

- 🐛 Fixed an issue that caused the emulator to freeze when certain commands were executed in rapid succession.
- 🐛 Resolved a problem that prevented the library from properly handling emulator responses containing special characters.

## Improvements

- 💡 Optimized the performance of the `GetDebugState()` method, reducing the time required to retrieve the emulator's current state.
- 💡 Improved the reliability of the `ResetEmulator()` method, ensuring a more consistent reset process.

## Compatibility

This release is compatible with .NET 9.0 and later.

## Upgrade Instructions

To upgrade to ViceMCP v0.3.2, simply update your project's package reference to the latest version. No other changes should be required.

## Feedback and Support

As always, we welcome your feedback and bug reports. If you encounter any issues or have suggestions for improving ViceMCP, please don't hesitate to reach out to our support team or submit an issue on the project's GitHub repository.