#!/usr/bin/env python3
"""Diagnose VICE binary monitor connection"""

import socket
import struct
import sys

def hex_dump(data, prefix=""):
    """Print hex dump of data"""
    for i in range(0, len(data), 16):
        hex_part = ' '.join(f'{b:02x}' for b in data[i:i+16])
        ascii_part = ''.join(chr(b) if 32 <= b < 127 else '.' for b in data[i:i+16])
        print(f"{prefix}{i:04x}: {hex_part:<48} {ascii_part}")

def send_raw_command(sock, command_bytes):
    """Send raw command and receive response"""
    print(f"\nSending {len(command_bytes)} bytes:")
    hex_dump(command_bytes, "  TX ")
    sock.send(command_bytes)
    
    # Read response header
    header = sock.recv(12)
    print(f"\nReceived header ({len(header)} bytes):")
    hex_dump(header, "  RX ")
    
    if len(header) >= 12:
        stx = header[0]
        version = header[1]
        body_len = struct.unpack('<I', header[2:6])[0]
        req_id = struct.unpack('<I', header[6:10])[0]
        resp_type = header[10]
        error_code = header[11]
        
        print(f"\n  STX: 0x{stx:02x}")
        print(f"  Version: 0x{version:02x}")
        print(f"  Body length: {body_len} (0x{body_len:x})")
        print(f"  Request ID: {req_id} (0x{req_id:08x})")
        print(f"  Response type: 0x{resp_type:02x}")
        print(f"  Error code: 0x{error_code:02x}")
        
        # Read body if present
        if body_len > 0:
            body = sock.recv(body_len)
            print(f"\nReceived body ({len(body)} bytes):")
            hex_dump(body, "  RX ")
            
            # Try to interpret as string
            try:
                text = body.decode('ascii', errors='replace')
                print(f"\n  As ASCII: {repr(text)}")
            except:
                pass

def test_vice():
    """Test VICE connection with various approaches"""
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect(('127.0.0.1', 6502))
    print("Connected to VICE on port 6502")
    
    # Test 1: Try minimal memory read command
    print("\n" + "="*60)
    print("Test 1: Minimal memory read (0x0400-0x0401)")
    
    # Build minimal command
    cmd = bytearray()
    cmd.append(0x02)  # STX
    cmd.append(0x02)  # Version
    
    # Body: request_id(4) + cmd(1) + side_effects(1) + start(2) + end(2) + memspace(1) + bank(2)
    body = bytearray()
    body.extend(struct.pack('<I', 1))      # Request ID = 1
    body.append(0x01)                       # Command = memory get
    body.append(0x00)                       # No side effects
    body.extend(struct.pack('<H', 0x0400)) # Start address
    body.extend(struct.pack('<H', 0x0401)) # End address
    body.append(0x00)                       # Memspace = main memory
    body.extend(struct.pack('<H', 0x0000)) # Bank = 0
    
    cmd.extend(struct.pack('<I', len(body)))  # Body length
    cmd.extend(body)
    
    send_raw_command(sock, cmd)
    
    # Test 2: Try with different API version
    print("\n" + "="*60)
    print("Test 2: Try API version 0x01")
    
    cmd2 = bytearray(cmd)
    cmd2[1] = 0x01  # Change API version
    send_raw_command(sock, cmd2)
    
    sock.close()
    print("\nTests completed")

if __name__ == "__main__":
    test_vice()