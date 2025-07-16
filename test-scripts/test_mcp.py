#!/usr/bin/env python3
"""Test script for ViceMCP - tests memory read/write operations"""

import json
import subprocess
import sys

def send_mcp_request(request):
    """Send a request to the MCP server and return the response"""
    # Start the MCP server as a subprocess
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", "ViceMCP/ViceMCP.csproj"],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True
    )
    
    # Send request
    proc.stdin.write(json.dumps(request) + '\n')
    proc.stdin.flush()
    
    # Read response
    response = proc.stdout.readline()
    
    # Terminate the process
    proc.terminate()
    
    return json.loads(response)

# Test 1: Read some well-known C64 memory locations
print("Test 1: Reading C64 screen memory at $0400-$0407")
request = {
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
        "name": "read_memory",
        "arguments": {
            "start_address": "0x0400",
            "end_address": "0x0407"
        }
    },
    "id": 1
}

response = send_mcp_request(request)
print(f"Response: {json.dumps(response, indent=2)}")

# Test 2: Write to screen memory
print("\nTest 2: Writing 'HELLO' to screen memory at $0400")
# ASCII values for HELLO: H=72, E=69, L=76, L=76, O=79
# C64 screen codes: H=8, E=5, L=12, L=12, O=15
request = {
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
        "name": "write_memory",
        "arguments": {
            "start_address": "0x0400",
            "data": "08050C0C0F"  # HELLO in C64 screen codes
        }
    },
    "id": 2
}

response = send_mcp_request(request)
print(f"Response: {json.dumps(response, indent=2)}")

# Test 3: Read back what we wrote
print("\nTest 3: Reading back the written data")
request = {
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
        "name": "read_memory",
        "arguments": {
            "start_address": "0x0400",
            "end_address": "0x0404"
        }
    },
    "id": 3
}

response = send_mcp_request(request)
print(f"Response: {json.dumps(response, indent=2)}")