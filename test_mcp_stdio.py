#!/usr/bin/env python3
"""Test ViceMCP server via stdio"""

import json
import subprocess
import time

def test_mcp_server():
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
    
    # First, send initialize request
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
    
    # Read response
    response_line = proc.stdout.readline()
    if response_line:
        print("Initialize response:", response_line)
        
        # Now test memory read
        read_request = {
            "jsonrpc": "2.0",
            "method": "tools/call",
            "params": {
                "name": "read_memory",
                "arguments": {
                    "startHex": "0400",
                    "endHex": "0407"
                }
            },
            "id": 2
        }
        
        print("\nSending read_memory request...")
        proc.stdin.write(json.dumps(read_request) + '\n')
        proc.stdin.flush()
        
        response_line = proc.stdout.readline()
        if response_line:
            print("Read memory response:", response_line)
            response = json.loads(response_line)
            if "result" in response:
                print("Memory contents:", response["result"])
    
    # Check stderr for any errors
    proc.terminate()
    errors = proc.stderr.read()
    if errors:
        print("\nServer errors:", errors)

if __name__ == "__main__":
    test_mcp_server()