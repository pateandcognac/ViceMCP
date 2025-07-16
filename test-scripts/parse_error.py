#!/usr/bin/env python3
"""Parse VICE error response"""

import struct

def parse_vice_error(data):
    """Parse VICE error response which seems to be length-prefixed strings"""
    pos = 0
    parts = []
    
    while pos < len(data):
        if pos + 2 > len(data):
            break
            
        # Read length (2 bytes, little-endian)
        length = struct.unpack('<H', data[pos:pos+2])[0]
        pos += 2
        
        if pos + length > len(data):
            break
            
        # Read data
        part = data[pos:pos+length]
        pos += length
        
        # Try to decode as string
        try:
            text = part.decode('ascii', errors='replace')
            parts.append(f"[{length}] '{text}'")
        except:
            parts.append(f"[{length}] {part.hex()}")
    
    return parts

# Test with the error we got
error_hex = "0a000303d4e5030000000301000003020a000304f30003372f00033837000305220003350c0003360100"
error_data = bytes.fromhex(error_hex)

print("Parsing VICE error response:")
parts = parse_vice_error(error_data)
for i, part in enumerate(parts):
    print(f"  Part {i}: {part}")

# Let's also check what those byte values might be
print("\nChecking specific sequences:")
print(f"  0x03d4 = {0x03d4} (decimal)")
print(f"  0xe503 = {0xe503} (decimal)")
print(f"  As big-endian: 0xd4e5 = {0xd4e5} (decimal)")

# The pattern might be different - let's try parsing as type-length-value
print("\nTrying different parse:")
pos = 0
while pos < len(error_data):
    if pos + 3 > len(error_data):
        break
    
    # Try: type(1) + length(2) + data
    type_byte = error_data[pos]
    length = struct.unpack('<H', error_data[pos+1:pos+3])[0]
    
    if pos + 3 + length > len(error_data):
        # Maybe it's: length(1) + data
        length = type_byte
        if pos + 1 + length <= len(error_data):
            data = error_data[pos+1:pos+1+length]
            print(f"  Pos {pos}: len={length}, data={data.hex()} '{data.decode('ascii', errors='replace')}'")
            pos += 1 + length
        else:
            pos += 1
    else:
        data = error_data[pos+3:pos+3+length]
        print(f"  Pos {pos}: type=0x{type_byte:02x}, len={length}, data={data.hex()}")
        pos += 3 + length