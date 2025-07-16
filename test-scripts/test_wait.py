#!/usr/bin/env python3
"""Test if VICE needs time after connection"""

import socket
import struct
import time

def test_with_delay():
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect(('127.0.0.1', 6502))
    print("Connected to VICE")
    
    # Wait a bit
    print("Waiting 1 second...")
    time.sleep(1)
    
    # Try to read what VICE sends on connect
    sock.settimeout(0.5)
    try:
        initial = sock.recv(1024)
        print(f"VICE sent on connect: {initial.hex()}")
        print(f"As text: {repr(initial)}")
    except socket.timeout:
        print("No initial data from VICE")
    
    sock.settimeout(None)
    
    # Now try our command
    print("\nSending memory read command...")
    cmd = bytearray()
    cmd.append(0x02)  # STX
    cmd.append(0x02)  # Version
    
    body = bytearray()
    body.extend(struct.pack('<I', 1))      # Request ID
    body.append(0x01)                       # Command = memory get
    body.append(0x00)                       # No side effects
    body.extend(struct.pack('<H', 0x0400)) # Start
    body.extend(struct.pack('<H', 0x0407)) # End
    body.append(0x00)                       # Memspace
    body.extend(struct.pack('<H', 0x0000)) # Bank
    
    cmd.extend(struct.pack('<I', len(body)))
    cmd.extend(body)
    
    sock.send(cmd)
    
    # Read response
    header = sock.recv(12)
    print(f"\nResponse header: {header.hex()}")
    
    if len(header) >= 12:
        resp_type = header[10]
        error_code = header[11]
        body_len = struct.unpack('<I', header[2:6])[0]
        
        print(f"Response type: 0x{resp_type:02x}, Error: 0x{error_code:02x}")
        
        if body_len > 0:
            body = sock.recv(body_len)
            print(f"Body ({len(body)} bytes): {body.hex()}")
            
            # If successful memory read, parse it
            if resp_type == 0x01 and error_code == 0x00:
                data_len = struct.unpack('<H', body[0:2])[0]
                memory_data = body[2:2+data_len]
                print(f"Memory data: {memory_data.hex()}")
                # Convert to readable format
                ascii_repr = ''.join(chr(b) if 32 <= b < 127 else '.' for b in memory_data)
                print(f"As ASCII: {ascii_repr}")
    
    sock.close()

if __name__ == "__main__":
    test_with_delay()