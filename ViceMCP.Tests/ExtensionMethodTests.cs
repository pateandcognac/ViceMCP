using System.IO;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using ViceMCP.ViceBridge.Commands;
using Xunit;

namespace ViceMCP.Tests
{
    public class ExtensionMethodTests
    {
        [Fact]
        public void BinaryReaderExtension_ReadBoolFromByte_Should_Return_True_For_1()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 1 });
            using var reader = new BinaryReader(stream);

            // Act
            var result = reader.ReadBoolFromByte();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void BinaryReaderExtension_ReadBoolFromByte_Should_Return_False_For_0()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 0 });
            using var reader = new BinaryReader(stream);

            // Act
            var result = reader.ReadBoolFromByte();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void BinaryReaderExtension_ReadBoolFromByte_Should_Return_False_For_Other_Values()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 2, 255, 100 });
            using var reader = new BinaryReader(stream);

            // Act & Assert
            reader.ReadBoolFromByte().Should().BeFalse(); // 2
            reader.ReadBoolFromByte().Should().BeFalse(); // 255
            reader.ReadBoolFromByte().Should().BeFalse(); // 100
        }

        [Fact]
        public void BinaryReaderExtension_ReadEnum_Should_Read_Enum_Value()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { (byte)MemSpace.MainMemory, (byte)MemSpace.Drive8, (byte)MemSpace.Drive9 });
            using var reader = new BinaryReader(stream);

            // Act
            var mem1 = reader.ReadEnum<MemSpace>();
            var mem2 = reader.ReadEnum<MemSpace>();
            var mem3 = reader.ReadEnum<MemSpace>();

            // Assert
            mem1.Should().Be(MemSpace.MainMemory);
            mem2.Should().Be(MemSpace.Drive8);
            mem3.Should().Be(MemSpace.Drive9);
        }

        [Fact]
        public void BinaryReaderExtension_ReadCpuOperation_Should_Read_CpuOperation()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 
                (byte)CpuOperation.Load, 
                (byte)CpuOperation.Store, 
                (byte)CpuOperation.Exec 
            });
            using var reader = new BinaryReader(stream);

            // Act
            var op1 = reader.ReadCpuOperation();
            var op2 = reader.ReadCpuOperation();
            var op3 = reader.ReadCpuOperation();

            // Assert
            op1.Should().Be(CpuOperation.Load);
            op2.Should().Be(CpuOperation.Store);
            op3.Should().Be(CpuOperation.Exec);
        }

        [Fact]
        public void BinaryWriterExtension_WriteBoolAsByte_Should_Write_1_For_True()
        {
            // Arrange
            var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Act
            writer.WriteBoolAsByte(true);
            writer.Flush();

            // Assert
            stream.ToArray().Should().Equal(new byte[] { 1 });
        }

        [Fact]
        public void BinaryWriterExtension_WriteBoolAsByte_Should_Write_0_For_False()
        {
            // Arrange
            var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Act
            writer.WriteBoolAsByte(false);
            writer.Flush();

            // Assert
            stream.ToArray().Should().Equal(new byte[] { 0 });
        }

        [Fact]
        public void SystemExtension_AsByte_Should_Return_1_For_True()
        {
            // Act
            var result = true.AsByte();

            // Assert
            result.Should().Be((byte)1);
        }

        [Fact]
        public void SystemExtension_AsByte_Should_Return_0_For_False()
        {
            // Act
            var result = false.AsByte();

            // Assert
            result.Should().Be((byte)0);
        }

        [Fact]
        public async Task SystemExtension_WaitForDataAsync_Should_Handle_Cancellation()
        {
            // This test requires a mock socket setup which is complex
            // For now, we'll test the cancellation path
            using var cts = new CancellationTokenSource();
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            
            // Cancel immediately
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => socket.WaitForDataAsync(cts.Token)
            );
        }

        [Fact]
        public async Task SystemExtension_WaitForDataAsync_Should_Handle_Disposed_Socket()
        {
            // Arrange
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => socket.WaitForDataAsync()
            );
        }
    }
}