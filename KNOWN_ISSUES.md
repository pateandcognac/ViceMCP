# Known Issues

## Auto-Resume After Write Operations

**Issue**: VICE emulator pauses (enters monitor mode) after write operations via the binary monitor protocol, and the auto-resume feature is not working as expected.

**Status**: Under investigation

**Details**:
- When performing memory writes or register updates through ViceMCP, VICE enters monitor mode and pauses execution
- The auto-resume feature attempts to send an `ExitCommand` after successful write operations
- Despite sending the exit command successfully, VICE remains paused
- Users must manually call `continue_execution` to resume VICE

**Workaround**:
After performing write operations, explicitly call the `continue_execution` tool:
```
write_memory startHex: "0xD020", dataHex: "05"
continue_execution
```

**Technical Notes**:
- The `ExitCommand` is being sent correctly with `ErrorCode.OK` response
- Added logging shows the auto-resume logic is executing
- VICE may have specific requirements for resuming execution that aren't documented in the binary monitor protocol
- The issue does not occur with read operations, only state-modifying operations

**Next Steps**:
- Investigate VICE source code for binary monitor protocol handling
- Test with different VICE versions
- Consider alternative approaches to auto-resume (e.g., using checkpoint commands)