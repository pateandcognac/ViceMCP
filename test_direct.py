#!/usr/bin/env python3
"""Direct test of VICE binary monitor protocol"""

import socket
import struct

def test_vice_connection():
    """Test direct connection to VICE binary monitor"""
    try:
        # Connect to VICE
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.connect(('127.0.0.1', 6502))
        print("Connected to VICE binary monitor on port 6502")
        
        # Build memory read command for $0400-$0407
        # Command structure:
        # STX (0x02)
        # API version (0x02)
        # Body length (4 bytes, little-endian)
        # Request ID (4 bytes, little-endian)
        # Command type (0x01 for memory get)
        # Side effects flag (0x00)
        # Start address (2 bytes, little-endian)
        # End address (2 bytes, little-endian)
        # Memspace (0x00 for main memory)
        # Bank ID (2 bytes, little-endian)
        
        request_id = 1
        start_addr = 0x0400
        end_addr = 0x0407
        
        # Build payload
        payload = bytearray()
        payload.append(0x00)  # side effects
        payload.extend(struct.pack('<H', start_addr))  # start address
        payload.extend(struct.pack('<H', end_addr))    # end address
        payload.append(0x00)  # memspace
        payload.extend(struct.pack('<H', 0x0000))      # bank ID
        
        # Build message
        body_length = 4 + 1 + len(payload)  # request ID + command + payload
        message = bytearray()
        message.append(0x02)  # STX
        message.append(0x02)  # API version
        message.extend(struct.pack('<I', body_length))
        message.extend(struct.pack('<I', request_id))
        message.append(0x01)  # command type (memory get)
        message.extend(payload)
        
        print(f"Sending message: {message.hex()}")
        sock.send(message)
        
        # Read response header
        header = sock.recv(12)
        print(f"Received header: {header.hex()}")
        
        # Parse header
        stx = header[0]
        api_version = header[1]
        response_body_length = struct.unpack('<I', header[2:6])[0]
        response_request_id = struct.unpack('<I', header[6:10])[0]
        response_type = header[10]
        error_code = header[11]
        
        print(f"STX: 0x{stx:02x}")
        print(f"API version: 0x{api_version:02x}")
        print(f"Body length: {response_body_length}")
        print(f"Request ID: {response_request_id}")
        print(f"Response type: 0x{response_type:02x}")
        print(f"Error code: 0x{error_code:02x}")
        
        # Read response body
        if response_body_length > 0:
            body = sock.recv(response_body_length)
            print(f"Received body: {body.hex()}")
            
            # Parse memory data
            data_length = struct.unpack('<H', body[0:2])[0]
            memory_data = body[2:2+data_length]
            print(f"Memory data ({data_length} bytes): {memory_data.hex()}")
        
        sock.close()
        print("Test completed successfully!")
        
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    test_vice_connection()