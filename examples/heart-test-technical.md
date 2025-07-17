# ViceMCP Heart Test - Technical Implementation

This document provides technical prompts with specific MCP tool calls for the heart test.

## 1. Simple Heart Display

```
Using ViceMCP tools, please:

1. Start VICE: 
   - Use `start_vice` with emulatorType: "x64sc"

2. Wait for it to boot:
   - Use `ping` to verify connection
   - Use `get_display` to see the READY prompt

3. Display a heart:
   - Use `write_memory` startHex: "0x0400", dataHex: "93" (heart character)
   - Use `write_memory` startHex: "0xD800", dataHex: "02" (red color)
   - Use `get_display` to verify

This puts a red heart in the top-left corner of the screen.
```

## 2. Fill Screen with Hearts - Machine Code

```
Please write this 6502 assembly routine to memory and execute it:

; Fill screen with hearts
; Code at $C000
LDA #$93      ; A9 93    - Load heart character
LDX #$00      ; A2 00    - Initialize index
loop1:
STA $0400,X   ; 9D 00 04 - Store to screen
STA $0500,X   ; 9D 00 05
STA $0600,X   ; 9D 00 06
STA $06E8,X   ; 9D E8 06
INX           ; E8       - Increment index
BNE loop1     ; D0 F2    - Branch if not zero
RTS           ; 60       - Return

Write it using:
- `write_memory` startHex: "0xC000", dataHex: "A9 93 A2 00 9D 00 04 9D 00 05 9D 00 06 9D E8 06 E8 D0 F2 60"

Execute using:
- `set_register` registerName: "PC", valueHex: "0xC000"
- `continue_execution`
```

## 3. Animated Color Cycling

```
Create a color cycling animation:

; Color cycle routine at $C100
start:
INC $C200     ; EE 00 C2 - Increment color counter
LDA $C200     ; AD 00 C2 - Load color
AND #$0F      ; 29 0F    - Mask to 0-15
TAX           ; AA       - Transfer to X
LDA #$00      ; A9 00    - Initialize index
TAY           ; A8       
loop:
TXA           ; 8A       - Get color from X
STA $D800,Y   ; 99 00 D8 - Store to color RAM
STA $D900,Y   ; 99 00 D9
STA $DA00,Y   ; 99 00 DA
STA $DAE8,Y   ; 99 E8 DA
INY           ; C8       - Increment Y
BNE loop      ; D0 F2    - Loop
; Delay loop
LDX #$40      ; A2 40
delay1:
LDY #$FF      ; A0 FF
delay2:
DEY           ; 88
BNE delay2    ; D0 FD
DEX           ; CA
BNE delay1    ; D0 F8
JMP start     ; 4C 00 C1 - Jump to start

Write using:
- `write_memory` startHex: "0xC100", dataHex: "EE 00 C2 AD 00 C2 29 0F AA A9 00 A8 8A 99 00 D8 99 00 D9 99 00 DA 99 E8 DA C8 D0 F2 A2 40 A0 FF 88 D0 FD CA D0 F8 4C 00 C1"
- `write_memory` startHex: "0xC200", dataHex: "00" (color counter)

Execute:
- `set_register` registerName: "PC", valueHex: "0xC100"
- `continue_execution`

Then get display multiple times to see animation:
- `get_display`
- Wait 1 second
- `get_display`
- Repeat several times
```

## 4. Heart Pattern Drawing

```
Draw a heart shape pattern in the center of the screen:

; Draw heart pattern at $C300
; Heart shape coordinates (row, col) stored as data
LDX #$00      ; A2 00
loop:
LDA heart_rows,X   ; BD 50 C3
CMP #$FF          ; C9 FF
BEQ done          ; F0 1B
TAY               ; A8
LDA #$00          ; A9 00
CLC               ; 18
; Calculate screen position (row * 40 + col)
CPY #$00          ; C0 00
BEQ skip_mult     ; F0 0A
mult_loop:
ADC #$28          ; 69 28 (add 40)
DEY               ; 88
BNE mult_loop     ; D0 FA
skip_mult:
CLC               ; 18
ADC heart_cols,X  ; 7D 70 C3
TAY               ; A8
LDA #$93          ; A9 93 (heart char)
STA $0400,Y       ; 99 00 04
LDA #$02          ; A9 02 (red)
STA $D800,Y       ; 99 00 D8
INX               ; E8
JMP loop          ; 4C 02 C3
done:
RTS               ; 60

; Heart pattern data (simplified)
heart_rows: .byte 10,10,10,11,11,11,11,12,12,12,13,13,14,$FF
heart_cols: .byte 19,20,21,18,19,21,22,18,20,22,19,21,20,$FF

This creates a small heart shape in the center of the screen.
```

## 5. Complete Test Sequence

```
Perform the complete heart test sequence:

1. Start VICE and verify connection
2. Clear screen: `write_memory` startHex: "0x0400", dataHex: "20" (space), repeat for full screen
3. Display single heart: as in step 1
4. Wait 2 seconds
5. Fill screen with hearts: execute routine from step 2
6. Wait 2 seconds  
7. Start color animation: execute routine from step 3
8. Let it run for 5 seconds
9. Stop execution: `reset` mode: "soft"
10. Get final display
11. Read some memory to verify:
    - `read_memory` startHex: "0x0400", endHex: "0x0427" (first line of screen)
    - `read_memory` startHex: "0xD800", endHex: "0xD827" (first line of color)

Expected results:
- Screen filled with heart characters (value $93)
- Colors cycling through 0-15
- No crashes or errors
```

## 6. Debugging Commands

```
If something goes wrong, use these debugging commands:

1. Check CPU state:
   - `get_registers`
   
2. Check specific memory:
   - `read_memory` startHex: "0xC000", endHex: "0xC01F" (your code)
   - `read_memory` startHex: "0x0400", endHex: "0x0410" (screen RAM)
   
3. Set breakpoint and step:
   - `set_checkpoint` startHex: "0xC000"
   - `continue_execution`
   - When it breaks: `step` count: 1
   - Check registers after each step
   
4. Search for heart characters:
   - `search_memory` startHex: "0x0400", endHex: "0x07FF", pattern: "93"
   
5. Compare memory regions:
   - `compare_memory` addr1Hex: "0x0400", addr2Hex: "0xD800", length: 40
```

## 7. BASIC Version

```
For a BASIC version, send this program via keyboard:

`send_keys` with:
10 PRINT CHR$(147):REM CLEAR SCREEN\n
20 PRINT "PRESS ANY KEY FOR HEARTS!"\n
30 GET A$:IF A$="" THEN 30\n
40 FOR I=0 TO 999\n
50 POKE 1024+I,147:REM HEART CHAR\n
60 POKE 55296+I,INT(RND(1)*16):REM RANDOM COLOR\n
70 NEXT I\n
80 GOTO 30\n
RUN\n

This creates an interactive heart display program.
```

These prompts provide specific MCP tool calls that an LLM can use to perform the heart test step by step.