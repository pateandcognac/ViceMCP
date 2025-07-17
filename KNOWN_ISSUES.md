# Known Issues

## ~~Auto-Resume After Write Operations~~ ✅ FIXED

**Issue**: VICE emulator pauses (enters monitor mode) after write operations via the binary monitor protocol, and the auto-resume feature is not working as expected.

**Status**: ✅ **FIXED** (as of v0.6.9)

**Solution**: 
The auto-resume feature now detects when VICE is paused by checking the jiffy clock (memory addresses $A0-$A2). If the jiffy clock hasn't changed between two reads, VICE is paused and an `ExitCommand` is automatically sent to resume execution.

**Previous Details**:
- When performing memory writes or register updates through ViceMCP, VICE enters monitor mode and pauses execution
- The auto-resume feature attempts to send an `ExitCommand` after successful write operations
- Despite sending the exit command successfully, VICE remains paused
- Users must manually call `continue_execution` to resume VICE

**How it was fixed**:
- Implemented jiffy clock detection to determine if VICE is paused
- Auto-resume now checks after EVERY successful command (except ExitCommand itself)
- Added proper timing delays to ensure reliable detection
- VICE now stays running after all operations