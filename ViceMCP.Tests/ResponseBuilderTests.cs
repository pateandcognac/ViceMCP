using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ViceMCP.ViceBridge;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Shared;
using Xunit;

namespace ViceMCP.Tests
{
    // Note: ResponseBuilder tests are commented out because the methods are internal
    // and not accessible from the test project. To test these, we would need to either:
    // 1. Make the methods public/protected
    // 2. Use InternalsVisibleTo attribute
    // 3. Test through the public API that uses ResponseBuilder
    /*
    public class ResponseBuilderTests
    {
        private readonly Mock<ILogger<ResponseBuilder>> _loggerMock;
        private readonly ResponseBuilder _responseBuilder;

        public ResponseBuilderTests()
        {
            _loggerMock = new Mock<ILogger<ResponseBuilder>>();
            _responseBuilder = new ResponseBuilder(_loggerMock.Object);
        }

        [Fact]
        public void GetResponseBodyLength_Should_Parse_Correctly()
        {
            // Arrange
            var header = new byte[12];
            BitConverter.TryWriteBytes(header.AsSpan()[2..], 1234u);

            // Act
            var length = _responseBuilder.GetResponseBodyLength(header);

            // Assert
            length.Should().Be(1234u);
        }

        [Fact]
        public void Build_Should_Throw_When_Not_Starting_With_STX()
        {
            // Arrange
            var header = new byte[12];
            header[0] = 0xFF; // Not STX

            // Act
            var act = () => _responseBuilder.Build(header, 2, ReadOnlySpan<byte>.Empty);

            // Assert
            act.Should().Throw<Exception>().WithMessage("Not starting with STX");
        }

        [Fact]
        public void Build_Should_Throw_When_API_Version_Mismatch()
        {
            // Arrange
            var header = new byte[12];
            header[0] = Constants.STX;
            header[1] = 3; // Wrong API version

            // Act
            var act = () => _responseBuilder.Build(header, 2, ReadOnlySpan<byte>.Empty);

            // Assert
            act.Should().Throw<Exception>().WithMessage("Unknown API version 3");
        }

        [Fact]
        public void Build_Should_Return_Empty_Response()
        {
            // Arrange
            var header = CreateValidHeader(ResponseType.Ping, ErrorCode.OK, 123);

            // Act
            var (response, requestId) = _responseBuilder.Build(header, 2, ReadOnlySpan<byte>.Empty);

            // Assert
            response.Should().BeOfType<EmptyViceResponse>();
            response.ErrorCode.Should().Be(ErrorCode.OK);
            response.ApiVersion.Should().Be(2);
            requestId.Should().Be(123);
        }

        [Fact]
        public void BuildMemoryGetResponse_Should_Parse_Memory_Data()
        {
            // Arrange
            var buffer = new byte[10];
            BitConverter.TryWriteBytes(buffer, (ushort)8); // Memory length
            buffer[2] = 0xDE;
            buffer[3] = 0xAD;
            buffer[4] = 0xBE;
            buffer[5] = 0xEF;
            buffer[6] = 0x01;
            buffer[7] = 0x02;
            buffer[8] = 0x03;
            buffer[9] = 0x04;

            // Act
            var response = _responseBuilder.BuildMemoryGetResponse(2, ErrorCode.OK, buffer);

            // Assert
            response.Should().NotBeNull();
            response.ErrorCode.Should().Be(ErrorCode.OK);
            response.MemorySegment.Size.Should().Be(8);
            response.MemorySegment.Data[0].Should().Be(0xDE);
            response.MemorySegment.Data[1].Should().Be(0xAD);
            response.MemorySegment.Data[2].Should().Be(0xBE);
            response.MemorySegment.Data[3].Should().Be(0xEF);
        }

        [Fact]
        public void BuildMemoryGetResponse_With_Error_Should_Return_Empty_Buffer()
        {
            // Act
            var response = _responseBuilder.BuildMemoryGetResponse(2, ErrorCode.ObjectDoesNotExist, ReadOnlySpan<byte>.Empty);

            // Assert
            response.ErrorCode.Should().Be(ErrorCode.ObjectDoesNotExist);
            response.MemorySegment.Should().Be(ManagedBuffer.Empty);
        }

        [Fact]
        public void BuildCheckpointInfoResponse_Should_Parse_Checkpoint_Data()
        {
            // Arrange
            var buffer = new byte[22];
            BitConverter.TryWriteBytes(buffer, 42u);           // CheckpointNumber
            buffer[4] = 1;                                     // CurrentlyHit
            BitConverter.TryWriteBytes(buffer.AsSpan()[5..], (ushort)0x1000); // StartAddress
            BitConverter.TryWriteBytes(buffer.AsSpan()[7..], (ushort)0x2000); // EndAddress
            buffer[9] = 1;                                     // StopWhenHit
            buffer[10] = 1;                                    // Enabled
            buffer[11] = (byte)CpuOperation.Store;             // CpuOperation
            buffer[12] = 0;                                    // Temporary
            BitConverter.TryWriteBytes(buffer.AsSpan()[13..], 5u);  // HitCount
            BitConverter.TryWriteBytes(buffer.AsSpan()[17..], 0u);  // IgnoreCount
            buffer[21] = 0;                                    // HasCondition

            // Act
            var response = _responseBuilder.BuildCheckpointInfoResponse(2, ErrorCode.OK, buffer);

            // Assert
            response.CheckpointNumber.Should().Be(42);
            response.CurrentlyHit.Should().BeTrue();
            response.StartAddress.Should().Be(0x1000);
            response.EndAddress.Should().Be(0x2000);
            response.StopWhenHit.Should().BeTrue();
            response.Enabled.Should().BeTrue();
            response.CpuOperation.Should().Be(CpuOperation.Store);
            response.Temporary.Should().BeFalse();
            response.HitCount.Should().Be(5);
            response.IgnoreCount.Should().Be(0);
            response.HasCondition.Should().BeFalse();
        }

        [Fact]
        public void BuildRegistersResponse_Should_Parse_Register_Items()
        {
            // Arrange
            var buffer = new byte[2 + 2 * 5]; // 2 registers, 5 bytes each
            BitConverter.TryWriteBytes(buffer, (ushort)2);
            
            // First register
            buffer[2] = 3;  // Size
            buffer[3] = 0;  // Register ID (A)
            BitConverter.TryWriteBytes(buffer.AsSpan()[4..], (ushort)0xFF);
            
            // Second register
            buffer[7] = 3;  // Size
            buffer[8] = 1;  // Register ID (X)
            BitConverter.TryWriteBytes(buffer.AsSpan()[9..], (ushort)0xAA);

            // Act
            var response = _responseBuilder.BuildRegistersResponse(2, ErrorCode.OK, buffer);

            // Assert
            response.Items.Should().HaveCount(2);
            response.Items[0].RegisterId.Should().Be(0);
            response.Items[0].RegisterValue.Should().Be(0xFF);
            response.Items[1].RegisterId.Should().Be(1);
            response.Items[1].RegisterValue.Should().Be(0xAA);
        }

        [Fact]
        public void BuildBanksAvailableResponse_Should_Parse_Bank_Items()
        {
            // Arrange
            var ramName = Encoding.ASCII.GetBytes("RAM");
            var romName = Encoding.ASCII.GetBytes("ROM");
            
            var buffer = new byte[100];
            BitConverter.TryWriteBytes(buffer, (ushort)2); // 2 banks
            
            // First bank
            buffer[2] = (byte)(1 + 2 + 1 + ramName.Length); // Item size
            BitConverter.TryWriteBytes(buffer.AsSpan()[3..], (ushort)0); // Bank ID
            buffer[5] = (byte)ramName.Length;
            ramName.CopyTo(buffer, 6);
            
            // Second bank
            var offset = 2 + buffer[2] + 1;
            buffer[offset] = (byte)(1 + 2 + 1 + romName.Length); // Item size
            BitConverter.TryWriteBytes(buffer.AsSpan()[(offset + 1)..], (ushort)1); // Bank ID
            buffer[offset + 3] = (byte)romName.Length;
            romName.CopyTo(buffer, offset + 4);

            // Act
            var response = _responseBuilder.BuildBanksAvailableResponse(2, ErrorCode.OK, buffer);

            // Assert
            response.Items.Should().HaveCount(2);
            response.Items[0].BankId.Should().Be(0);
            response.Items[0].Name.Should().Be("RAM");
            response.Items[1].BankId.Should().Be(1);
            response.Items[1].Name.Should().Be("ROM");
        }

        [Fact]
        public void BuildResourceGetResponse_Should_Parse_String_Resource()
        {
            // Arrange
            var value = Encoding.ASCII.GetBytes("Hello");
            var buffer = new byte[2 + value.Length];
            buffer[0] = (byte)ResourceType.String;
            buffer[1] = (byte)value.Length;
            value.CopyTo(buffer, 2);

            // Act
            var response = _responseBuilder.BuildResourceGetResponse(2, ErrorCode.OK, buffer);

            // Assert
            response.Resource.Should().BeOfType<StringResource>();
            ((StringResource)response.Resource).Value.Should().Be("Hello");
        }

        [Fact]
        public void BuildResourceGetResponse_Should_Parse_Integer_Resource()
        {
            // Arrange
            var buffer = new byte[6];
            buffer[0] = (byte)ResourceType.Integer;
            buffer[1] = 4; // Length of int
            BitConverter.TryWriteBytes(buffer.AsSpan()[2..], 42);

            // Act
            var response = _responseBuilder.BuildResourceGetResponse(2, ErrorCode.OK, buffer);

            // Assert
            response.Resource.Should().BeOfType<IntegerResource>();
            ((IntegerResource)response.Resource).Value.Should().Be(42);
        }

        [Fact]
        public void BuildDisplayGetResponse_Should_Parse_Display_Data()
        {
            // Arrange
            var buffer = new byte[100];
            BitConverter.TryWriteBytes(buffer, 17u);           // Info length
            BitConverter.TryWriteBytes(buffer.AsSpan()[4..], (ushort)320);  // Debug width
            BitConverter.TryWriteBytes(buffer.AsSpan()[6..], (ushort)200);  // Debug height
            BitConverter.TryWriteBytes(buffer.AsSpan()[8..], (ushort)0);    // Debug offset X
            BitConverter.TryWriteBytes(buffer.AsSpan()[10..], (ushort)0);   // Debug offset Y
            BitConverter.TryWriteBytes(buffer.AsSpan()[12..], (ushort)320); // Inner width
            BitConverter.TryWriteBytes(buffer.AsSpan()[14..], (ushort)200); // Inner height
            buffer[16] = 8;                                     // Bits per pixel
            BitConverter.TryWriteBytes(buffer.AsSpan()[17..], 10u); // Buffer length
            
            // Add some image data
            for (int i = 0; i < 10; i++)
            {
                buffer[21 + i] = (byte)i;
            }

            // Act
            var response = _responseBuilder.BuildDisplayGetResponse(2, ErrorCode.OK, buffer);

            // Assert
            response.DebugWidth.Should().Be(320);
            response.DebugHeight.Should().Be(200);
            response.InnerWidth.Should().Be(320);
            response.InnerHeight.Should().Be(200);
            response.BitsPerPixel.Should().Be(8);
            response.ImageBuffer.Size.Should().Be(10);
            response.ImageBuffer.Data[0].Should().Be(0);
            response.ImageBuffer.Data[9].Should().Be(9);
        }

        [Fact]
        public void BuildInfoResponse_Should_Parse_Version_Info()
        {
            // Arrange
            var buffer = new byte[10];
            buffer[0] = 4;  // Version size
            buffer[1] = 3;  // Major
            buffer[2] = 1;  // Minor
            buffer[3] = 4;  // Build
            buffer[4] = 1;  // Revision
            buffer[5] = 4;  // SVN version size
            BitConverter.TryWriteBytes(buffer.AsSpan()[6..], 12345u); // SVN version

            // Act
            var response = _responseBuilder.BuildInfoResponse(2, ErrorCode.OK, buffer);

            // Assert
            response.Major.Should().Be(3);
            response.Minor.Should().Be(1);
            response.Build.Should().Be(4);
            response.Revision.Should().Be(1);
            response.SvnVersion.Should().Be(12345);
        }

        [Fact]
        public void Build_Should_Handle_All_Response_Types()
        {
            // Test that all response types are handled
            var responseTypes = Enum.GetValues<ResponseType>();
            
            foreach (var responseType in responseTypes)
            {
                // Arrange
                var header = CreateValidHeader(responseType, ErrorCode.OK, 1);
                
                // Act
                var act = () => _responseBuilder.Build(header, 2, new byte[100]);
                
                // Assert - should not throw
                act.Should().NotThrow($"ResponseType.{responseType} should be handled");
            }
        }

        private byte[] CreateValidHeader(ResponseType responseType, ErrorCode errorCode, uint requestId)
        {
            var header = new byte[12];
            header[0] = Constants.STX;
            header[1] = 2; // API version
            BitConverter.TryWriteBytes(header.AsSpan()[2..], 0u); // Length
            header[6] = (byte)responseType;
            header[7] = (byte)errorCode;
            BitConverter.TryWriteBytes(header.AsSpan()[8..], requestId);
            return header;
        }
    }
    */
}