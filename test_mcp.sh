#!/bin/bash

# Test ViceMCP locally by sending MCP protocol messages

# Initialize message
INIT_MSG='{
  "jsonrpc": "2.0",
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {
      "name": "test-client",
      "version": "1.0.0"
    }
  },
  "id": 1
}'

# List tools message
LIST_TOOLS_MSG='{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {},
  "id": 2
}'

echo "Testing ViceMCP MCP Server..."
echo "Sending initialize request..."

# Send messages to ViceMCP
(echo "$INIT_MSG"; sleep 1; echo "$LIST_TOOLS_MSG"; sleep 1) | dotnet run --project ViceMCP/ViceMCP.csproj 2>&1 | grep -E '"method"|"result"|"tools"'