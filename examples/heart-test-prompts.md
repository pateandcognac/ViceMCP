# ViceMCP Heart Test - LLM Prompts

This document contains prompts to guide an LLM through performing the classic Commodore 64 "heart test" using ViceMCP. This serves as our "Hello, World" example.

## Prerequisites Prompt

```
I need to test ViceMCP by performing the classic C64 heart test. First, please:
1. Check if VICE is running using the ping tool
2. If not running, start the C64 emulator (x64sc) 
3. Get the current display to see the initial state
```

## Heart Character Setup Prompt

```
Now let's set up to display hearts on the C64 screen. Please:
1. Write the PETSCII heart character (value 147 or $93) to screen memory at address $0400
2. Write the color red (value 2) to color memory at address $D800
3. Get the display to see the heart
```

## Fill Screen with Hearts Prompt

```
Great! Now let's fill the entire screen with colored hearts. Please:
1. Write a small machine language routine at $C000 that:
   - Loads the heart character ($93) into the accumulator
   - Fills all 1000 screen positions ($0400-$07E7) with hearts
   - Loads a color value into the accumulator  
   - Fills all 1000 color memory positions ($D800-$DBE7) with that color
   - Returns with RTS
2. Execute this routine by setting PC to $C000 and continuing execution
3. Get the display to show the result
```

## Animated Heart Colors Prompt

```
Let's make the hearts change colors! Please:
1. Write a machine language routine at $C100 that:
   - Uses a memory location (e.g., $C200) as a color counter
   - Increments the color counter
   - Masks it to values 0-15 (AND #$0F)
   - Fills color memory ($D800-$DBE7) with this color
   - Includes a delay loop for visibility
   - Jumps back to the start for continuous animation
2. Set the initial color counter value at $C200 to 0
3. Execute this routine by setting PC to $C100 and continuing execution
4. Get the display a few times over several seconds to see the animation
```

## Interactive Heart Test Prompt

```
For the final test, let's make it interactive. Please:
1. Write a BASIC program that:
   - Clears the screen
   - Prints "PRESS ANY KEY FOR HEARTS!"
   - Waits for a keypress
   - Fills the screen with hearts when a key is pressed
   - Changes colors each time a key is pressed
2. You can do this by:
   - Writing BASIC tokens to memory starting at $0801
   - Or sending keyboard input to type the program
3. Run the program and demonstrate it working
```

## Complete Heart Test Demo Prompt

```
Please perform a complete ViceMCP heart test demonstration:
1. Start VICE if not running
2. Clear the screen
3. Display a single red heart at the center of the screen
4. Wait a moment, then fill the entire screen with hearts
5. Make the hearts cycle through all 16 colors
6. Create a pattern where hearts appear in a wave from top to bottom
7. Get the final display showing the result

This will demonstrate basic memory operations, color manipulation, and machine language execution through ViceMCP.
```

## Troubleshooting Prompt

```
If the heart test isn't working correctly, please:
1. Check the current CPU registers and PC location
2. Examine memory at $0400 (screen) and $D800 (color) 
3. Verify the machine code was written correctly
4. Set a breakpoint at the routine start address
5. Step through the code instruction by instruction
6. Identify and fix any issues
```

## Advanced Heart Test Prompt

```
For an advanced demo, create a heart animation that:
1. Starts with an empty blue screen
2. Draws a large heart shape made of heart characters in the center
3. Makes the heart "beat" by changing its size
4. Adds smaller hearts that radiate outward from the main heart
5. Uses different colors for visual effect
6. Includes sound effects using the SID chip (optional)

This demonstrates advanced memory manipulation and creative use of the character screen.
```

## Verification Prompt

```
To verify the heart test completed successfully:
1. Take a screenshot of the final display
2. Read memory at $0400-$0427 (first 40 screen positions) and verify it contains heart characters ($93)
3. Read memory at $D800-$D827 (first 40 color positions) and verify it contains color values
4. Confirm no errors were reported during execution
5. Summarize what was accomplished in the test
```

---

## Example Usage

To use these prompts with an LLM that has access to ViceMCP:

1. Start with the Prerequisites Prompt
2. Follow with the Heart Character Setup Prompt to verify basic functionality
3. Progress through the Fill Screen and Animation prompts
4. Use the Troubleshooting Prompt if issues arise
5. End with the Verification Prompt to confirm success

This provides a structured way to test ViceMCP functionality while creating a visually appealing demo that's easy to verify.