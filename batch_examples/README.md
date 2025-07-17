# Batch Command Examples

This directory contains examples of using the `execute_batch` MCP tool to perform multiple VICE commands in a single operation.

## Usage

To use a batch command, call the `execute_batch` MCP tool with the JSON content from any of these files:

```bash
# Example MCP call (conceptual)
execute_batch --commandsJson "$(cat batch_examples/heart_sprite_example.json)"
```

## Examples

### heart_sprite_example.json
Sets up a complete C64 screen with a red heart sprite in the center:
- Sets border and background colors to black
- Clears the screen
- Defines heart sprite data
- Enables and positions sprite 0
- Sets sprite color to red

### screen_setup_example.json
Sets up a colorful screen with text:
- Sets border to black, background to blue
- Sets multicolor registers
- Clears screen with spaces
- Sets color RAM to white
- Writes "HELLO WORLD!" in light blue

## Command Format

Each batch command is a JSON array of command specifications:

```json
[
  {
    "command": "write_memory",
    "parameters": {
      "startHex": "d020",
      "dataHex": "00"
    },
    "description": "Set border color to black"
  }
]
```

### Fields:
- `command`: The name of the MCP tool to execute
- `parameters`: Object containing the parameters for the command
- `description`: Optional description of what the command does

## Available Commands

All existing ViceMCP tools can be used in batch mode:
- `write_memory`
- `read_memory`
- `fill_memory`
- `copy_memory`
- `search_memory`
- `compare_memory`
- `set_register`
- `get_registers`
- `step`
- `continue_execution`
- `reset`
- `ping`
- `get_info`
- `get_banks`
- `set_checkpoint`
- `list_checkpoints`
- `delete_checkpoint`
- `toggle_checkpoint`
- `get_display`
- `send_keys`
- `load_program`
- `save_memory`
- `quit_vice`
- `start_vice`

## Error Handling

The batch executor supports two modes:
- `failFast: true` (default): Stops on first error
- `failFast: false`: Continues executing commands even if some fail

## Response Format

The batch executor returns a JSON response with:
- `total_commands`: Number of commands in the batch
- `successful_commands`: Number of commands that succeeded
- `failed_commands`: Number of commands that failed
- `results`: Array of individual command results
- `execution_time_ms`: Total execution time

Example response:
```json
{
  "total_commands": 3,
  "successful_commands": 3,
  "failed_commands": 0,
  "results": [
    {
      "success": true,
      "result": "Wrote 1 bytes to $D020",
      "command": "write_memory",
      "description": "Set border color to black"
    }
  ],
  "execution_time_ms": 45
}
```