#!/usr/bin/env python3
"""Test VICE with ping command first"""

import socket
import struct

def hex_dump(data, prefix=""):
    """Print hex dump of data"""
    for i in range(0, len(data), 16):
        hex_part = ' '.join(f'{b:02x}' for b in data[i:i+16])
        ascii_part = ''.join(chr(b) if 32 <= b < 127 else '.' for b in data[i:i+16])
        print(f"{prefix}{i:04x}: {hex_part:<48} {ascii_part}")

def send_command(sock, command_id, payload=b''):
    """Send a command and return the response"""
    # Build message
    msg = bytearray()
    msg.append(0x02)  # STX
    msg.append(0x02)  # API version
    
    # Body = request_id(4) + command(1) + payload
    body = bytearray()
    body.extend(struct.pack('<I', 1))  # Request ID
    body.append(command_id)
    body.extend(payload)
    
    msg.extend(struct.pack('<I', len(body)))  # Body length
    msg.extend(body)
    
    print(f"\nSending command 0x{command_id:02x}:")
    hex_dump(msg, "  TX ")
    
    sock.send(msg)
    
    # Read response header
    header = sock.recv(12)
    print(f"\nResponse header:")
    hex_dump(header, "  RX ")
    
    if len(header) >= 12:
        stx = header[0]
        version = header[1]
        body_len = struct.unpack('<I', header[2:6])[0]
        req_id = struct.unpack('<I', header[6:10])[0]
        resp_type = header[10]
        error_code = header[11]
        
        print(f"\n  Response type: 0x{resp_type:02x}, Error: 0x{error_code:02x}")
        print(f"  Request ID: {req_id} (0x{req_id:08x})")
        
        # Read body if present
        if body_len > 0:
            body = sock.recv(body_len)
            print(f"\nResponse body ({len(body)} bytes):")
            hex_dump(body, "  RX ")
            
            # For successful responses, try to parse
            if error_code == 0x00:
                return (resp_type, body)
            else:
                print(f"  Error details: {body}")
        
        return (resp_type, error_code)
    
    return (None, None)

def test_vice():
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect(('127.0.0.1', 6502))
    print("Connected to VICE on port 6502")
    
    # Test 1: Send ping (0x81)
    print("\n" + "="*60)
    print("Test 1: Ping command")
    send_command(sock, 0x81)
    
    # Test 2: Get VICE info (0x85)
    print("\n" + "="*60)
    print("Test 2: VICE info command")
    resp_type, data = send_command(sock, 0x85)
    if data and isinstance(data, bytes):
        # Parse VICE info response
        pos = 0
        # Version major (1 byte)
        if pos < len(data):
            major = data[pos]
            pos += 1
            print(f"  Version major: {major}")
        # Version minor (1 byte)
        if pos < len(data):
            minor = data[pos]
            pos += 1
            print(f"  Version minor: {minor}")
        # Version patch (1 byte)  
        if pos < len(data):
            patch = data[pos]
            pos += 1
            print(f"  Version patch: {patch}")
        # Version string (null-terminated)
        if pos < len(data):
            null_pos = data.find(0, pos)
            if null_pos >= 0:
                version_str = data[pos:null_pos].decode('ascii', errors='replace')
                print(f"  Version string: '{version_str}'")
    
    # Test 3: Try memory read after successful ping
    print("\n" + "="*60)
    print("Test 3: Memory read (0x0400-0x0407)")
    
    payload = bytearray()
    payload.append(0x00)  # No side effects
    payload.extend(struct.pack('<H', 0x0400))  # Start
    payload.extend(struct.pack('<H', 0x0407))  # End
    payload.append(0x00)  # Memspace = main memory
    payload.extend(struct.pack('<H', 0x0000))  # Bank = 0
    
    resp_type, data = send_command(sock, 0x01, payload)
    if data and isinstance(data, bytes) and len(data) >= 2:
        data_len = struct.unpack('<H', data[0:2])[0]
        if len(data) >= 2 + data_len:
            memory_data = data[2:2+data_len]
            print(f"  Memory data ({data_len} bytes): {memory_data.hex()}")
    
    sock.close()

if __name__ == "__main__":
    test_vice()