#!/usr/bin/env python3
"""Test writing to VICE memory via MCP"""

import json
import subprocess
import time

# Start the MCP server
proc = subprocess.Popen(
    ["dotnet", "run", "--project", "ViceMCP/ViceMCP.csproj"],
    stdin=subprocess.PIPE,
    stdout=subprocess.PIPE,
    stderr=subprocess.PIPE,
    text=True,
    bufsize=1
)

# Give it time to start
time.sleep(2)

# Initialize
init_request = {
    "jsonrpc": "2.0",
    "method": "initialize",
    "params": {
        "protocolVersion": "2024-11-05",
        "capabilities": {},
        "clientInfo": {
            "name": "test-client",
            "version": "1.0"
        }
    },
    "id": 1
}

print("Sending initialize request...")
proc.stdin.write(json.dumps(init_request) + '\n')
proc.stdin.flush()
response = proc.stdout.readline()
print("Initialize response:", response)

# Write "HELLO" to screen memory
# PETSCII: H=08, E=05, L=0C, L=0C, O=0F
write_request = {
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
        "name": "write_memory",
        "arguments": {
            "startHex": "0400",
            "dataHex": "08 05 0C 0C 0F"
        }
    },
    "id": 2
}

print("\nSending write_memory request...")
proc.stdin.write(json.dumps(write_request) + '\n')
proc.stdin.flush()
response = proc.stdout.readline()
print("Write memory response:", response)

# Read it back
read_request = {
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
        "name": "read_memory",
        "arguments": {
            "startHex": "0400",
            "endHex": "0404"
        }
    },
    "id": 3
}

print("\nSending read_memory request to verify...")
proc.stdin.write(json.dumps(read_request) + '\n')
proc.stdin.flush()
response = proc.stdout.readline()
print("Read memory response:", response)

proc.terminate()
print("\nCheck your VICE screen - you should see 'HELLO' in the top-left corner!")