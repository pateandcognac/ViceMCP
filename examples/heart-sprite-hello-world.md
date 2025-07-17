# ViceMCP "Hello World" - Heart Sprite Test

## Single Prompt for Heart Sprite Test

```
Please run the ViceMCP heart sprite test by:

1. Starting VICE (x64sc) if not already running
2. Clearing the screen by filling $0400-$07E7 with spaces ($20)
3. Setting black background ($00 to $D021) and black border ($00 to $D020)  
4. Writing this heart sprite pattern to $0340:
   00 7E 00 01 FF 80 03 FF C0 07 FF E0 07 FF E0 0F FF F0 0F FF F0 0F FF F0 0F FF F0 0F FF F0 0F FF F0 07 FF E0 07 FF E0 03 FF C0 01 FF 80 00 FF 00 00 7E 00 00 3C 00 00 18 00 00 00 00
5. Configuring sprite 0: pointer $0D at $07F8, color red ($02) at $D027, position ($A0,$80) at $D000-D001
6. Enabling sprite 0 by writing $01 to $D015
7. Getting the display to show the red heart sprite

This is our "Hello World" - a red heart sprite on black background.
```

## Copy-Paste Version for LLMs

```
Run these exact ViceMCP commands to display a red heart sprite:

start_vice emulatorType: "x64sc"
fill_memory startHex: "0x0400", endHex: "0x07E7", pattern: "20"
write_memory startHex: "0xD021", dataHex: "00"
write_memory startHex: "0xD020", dataHex: "00"  
write_memory startHex: "0x0340", dataHex: "00 00 00 00 00 00 03 C7 80 0F FF E0 1F FF F0 3F FF F8 3F FF F8 3F FF F8 3F FF F8 3F FF F8 1F FF F0 1F FF F0 0F FF E0 07 FF C0 03 FF 80 01 FF 00 00 FE 00 00 7C 00 00 38 00 00 10 00 00 00 00"
write_memory startHex: "0x07F8", dataHex: "0D"
write_memory startHex: "0xD027", dataHex: "02"
write_memory startHex: "0xD000", dataHex: "A0"
write_memory startHex: "0xD001", dataHex: "80"
write_memory startHex: "0xD015", dataHex: "01"
get_display
```

## Expected Result

The display should show:
- Clear screen (no text)
- Black background
- Black border
- Red heart-shaped sprite in the center of the screen

This confirms ViceMCP is working correctly and can control VICE to create graphics.