# ViceMCP Sprite Heart Test - LLM Prompts

This document contains prompts for an LLM to perform the sprite heart test that displays a red heart sprite on a black background with colored borders.

## Complete Sprite Heart Test Prompt

```
Please perform the ViceMCP sprite heart test:

1. Start VICE if not already running
2. Set the screen to black background with a colored border
3. Create a heart-shaped sprite pattern in memory
4. Configure sprite 0 to display the heart
5. Set the sprite color to red
6. Position the sprite in the center of the screen
7. Enable the sprite
8. Get the display to verify the red heart is visible
```

## Detailed Step-by-Step Prompt

```
I need you to create a red heart sprite on the C64. Please follow these steps:

1. Start the C64 emulator using start_vice if not running

2. Set up the display colors:
   - Set background color to black: write $00 to $D021
   - Set border color to light blue: write $0E to $D020
   
3. Create the heart sprite pattern at $0340 (sprite 0 data):
   - The pattern should be a 24x21 pixel heart shape
   - Write the heart pattern bytes (63 bytes total)
   
4. Configure sprite 0:
   - Set sprite 0 pointer: write $0D to $07F8 (points to $0340)
   - Set sprite 0 color to red: write $02 to $D027
   - Set sprite 0 X position: write $A0 to $D000 
   - Set sprite 0 Y position: write $80 to $D001
   
5. Enable sprite 0:
   - Write $01 to $D015 (sprite enable register)
   
6. Get the display to see the red heart sprite

This should show a red heart sprite on a black screen with a light blue border.
```

## Quick Test Prompt

```
Run the standard ViceMCP heart sprite test:
1. Execute the heart sprite setup routine that creates a red heart on black background
2. Verify the sprite is visible and properly colored
3. Take a screenshot of the display
```

## Technical Implementation Prompt

```
Using ViceMCP tools, create a heart sprite:

1. Set colors:
   - write_memory startHex: "0xD021", dataHex: "00" (black background)
   - write_memory startHex: "0xD020", dataHex: "0E" (light blue border)

2. Write heart sprite pattern:
   - write_memory startHex: "0x0340", dataHex: "00 7E 00 01 FF 80 03 FF C0 07 FF E0 07 FF E0 0F FF F0 0F FF F0 0F FF F0 0F FF F0 0F FF F0 0F FF F0 07 FF E0 07 FF E0 03 FF C0 01 FF 80 00 FF 00 00 7E 00 00 3C 00 00 18 00 00 00 00"

3. Configure sprite:
   - write_memory startHex: "0x07F8", dataHex: "0D" (sprite 0 pointer to $0340)
   - write_memory startHex: "0xD027", dataHex: "02" (sprite 0 color = red)
   - write_memory startHex: "0xD000", dataHex: "A0" (sprite 0 X position)
   - write_memory startHex: "0xD001", dataHex: "80" (sprite 0 Y position)
   - write_memory startHex: "0xD015", dataHex: "01" (enable sprite 0)

4. Verify:
   - get_display
```

## Animated Heart Sprite Prompt

```
Make the heart sprite animate (beat):

1. Create the static heart sprite as above
2. Write a machine language routine that:
   - Toggles sprite expand registers to make the heart "beat"
   - Changes the border color in sync with the beat
   - Uses a delay loop for timing
3. Execute the animation routine
4. Get multiple display snapshots to show the animation
```

## Heart Sprite Movement Prompt

```
Make the heart sprite move across the screen:

1. Set up the heart sprite as before
2. Create a routine at $C000 that:
   - Reads current X position from $D000
   - Increments it
   - Handles the 9th bit in $D010 for positions > 255
   - Writes back to $D000
   - Includes a delay
   - Loops continuously
3. Execute the routine and observe the heart moving
```

## Troubleshooting Sprite Prompt

```
If the heart sprite isn't visible, debug by:

1. Check VIC registers:
   - read_memory startHex: "0xD000", endHex: "0xD030" (all sprite registers)
   - Verify $D015 has bit 0 set (sprite 0 enabled)
   
2. Check sprite data:
   - read_memory startHex: "0x0340", endHex: "0x037F" (sprite 0 data)
   - Verify the heart pattern is present
   
3. Check sprite pointer:
   - read_memory startHex: "0x07F8", endHex: "0x07FF" 
   - Verify sprite 0 pointer is $0D (pointing to $0340)
   
4. Check colors:
   - Verify $D027 = $02 (red sprite)
   - Verify $D021 = $00 (black background)
```

## Complete Sprite Heart Test with Effects

```
Create an impressive heart sprite demo:

1. Clear the screen to black with colored border
2. Create and display the red heart sprite in the center
3. Add these effects in sequence:
   - Make the heart beat (expand/contract)
   - Make the heart move in a circle
   - Change border colors to match the heartbeat
   - Add a second heart sprite in a different color
   - Make both hearts orbit around each other
4. Get display snapshots at each stage
```

## Verification Prompt

```
Verify the sprite heart test succeeded:

1. Take a final screenshot showing the heart sprite
2. Read and verify:
   - Sprite 0 is enabled: $D015 & $01 = $01
   - Sprite 0 color is red: $D027 = $02
   - Background is black: $D021 = $00
   - Border is colored: $D020 = $0E (or other non-black)
   - Sprite data contains heart pattern at $0340
3. Confirm the visual output shows a red heart sprite
```

---

This is the sprite-based heart test that creates the visual we've been working with - a red heart sprite on a black background with colored borders.