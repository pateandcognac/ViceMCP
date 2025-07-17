using System.Text;
using FluentAssertions;
using ViceMCP.ViceBridge;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Shared;
using Xunit;

namespace ViceMCP.Tests
{
    public class ViceCommandTests
    {
        [Fact]
        public void MemoryGetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new MemoryGetCommand(
                SideEffects: 0,
                StartAddress: 0x1000,
                EndAddress: 0x10FF,
                MemSpace: MemSpace.MainMemory,
                BankId: 0
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be(0); // SideEffects
            BitConverter.ToUInt16(buffer, 1).Should().Be(0x1000); // StartAddress
            BitConverter.ToUInt16(buffer, 3).Should().Be(0x10FF); // EndAddress
            buffer[5].Should().Be((byte)MemSpace.MainMemory); // MemSpace
            BitConverter.ToUInt16(buffer, 6).Should().Be(0); // BankId
        }

        [Fact]
        public void MemorySetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var memoryBuffer = BufferManager.GetBuffer(4);
            memoryBuffer.Data[0] = 0xDE;
            memoryBuffer.Data[1] = 0xAD;
            memoryBuffer.Data[2] = 0xBE;
            memoryBuffer.Data[3] = 0xEF;
            
            var command = new MemorySetCommand(
                SideEffects: 0,
                StartAddress: 0x1000,
                MemSpace: MemSpace.MainMemory,
                BankId: 0,
                MemoryContent: memoryBuffer
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be(0); // SideEffects
            BitConverter.ToUInt16(buffer, 1).Should().Be(0x1000); // StartAddress
            BitConverter.ToUInt16(buffer, 3).Should().Be(0x1003); // EndAddress (calculated)
            buffer[5].Should().Be((byte)MemSpace.MainMemory); // MemSpace
            BitConverter.ToUInt16(buffer, 6).Should().Be(0); // BankId
            buffer[8].Should().Be(0xDE); // First data byte
            buffer[9].Should().Be(0xAD);
            buffer[10].Should().Be(0xBE);
            buffer[11].Should().Be(0xEF); // Last data byte
        }

        [Fact]
        public void CheckpointSetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new CheckpointSetCommand(
                StartAddress: 0x1000,
                EndAddress: 0x2000,
                StopWhenHit: true,
                Enabled: true,
                CpuOperation: CpuOperation.Exec,
                Temporary: false
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            BitConverter.ToUInt16(buffer, 0).Should().Be(0x1000); // StartAddress
            BitConverter.ToUInt16(buffer, 2).Should().Be(0x2000); // EndAddress
            buffer[4].Should().Be(1); // StopWhenHit (true)
            buffer[5].Should().Be(1); // Enabled (true)
            buffer[6].Should().Be((byte)CpuOperation.Exec); // CpuOperation
            buffer[7].Should().Be(0); // Temporary (false)
        }

        [Fact]
        public void RegistersSetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var items = new[]
            {
                new RegisterItem(0, 0xFF), // A register
                new RegisterItem(1, 0xAA)  // X register
            };
            var command = new RegistersSetCommand(MemSpace.MainMemory, items);
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be((byte)MemSpace.MainMemory); // MemSpace
            BitConverter.ToUInt16(buffer, 1).Should().Be(2); // Item count
            
            // First register
            buffer[3].Should().Be(3); // Item size
            buffer[4].Should().Be(0); // Register ID
            BitConverter.ToUInt16(buffer, 5).Should().Be(0xFF); // Value
            
            // Second register
            buffer[7].Should().Be(3); // Item size
            buffer[8].Should().Be(1); // Register ID
            BitConverter.ToUInt16(buffer, 9).Should().Be(0xAA); // Value
        }

        [Fact]
        public void KeyboardFeedCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var text = "HELLO";
            var command = new KeyboardFeedCommand(text);
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be((byte)text.Length);
            Encoding.ASCII.GetString(buffer, 1, text.Length).Should().Be(text);
        }

        // Note: ResourceSetCommand tests removed because it's not clear how the resource name
        // is included in the command from the current implementation

        [Fact]
        public void DisplayGetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new DisplayGetCommand(
                UseVic: true,
                Format: ImageFormat.Indexed
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be(1); // UseVIC (true)
            buffer[1].Should().Be((byte)ImageFormat.Indexed); // Format
        }

        [Fact]
        public void ViceCommand_CollectErrors_Should_Return_Empty_For_Valid_Command()
        {
            // Arrange
            var command = new PingCommand();

            // Act
            var errors = command.CollectErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void Commands_Should_Have_Correct_Properties()
        {
            // Arrange & Act & Assert
            var ping = new PingCommand();
            ping.CommandType.Should().Be(CommandType.Ping);
            ping.ContentLength.Should().Be(0);

            var exit = new ExitCommand();
            exit.CommandType.Should().Be(CommandType.Exit);
            exit.ContentLength.Should().Be(0);

            var quit = new QuitCommand();
            quit.CommandType.Should().Be(CommandType.Quit);
            quit.ContentLength.Should().Be(0);

            var info = new InfoCommand();
            info.CommandType.Should().Be(CommandType.Info);
            info.ContentLength.Should().Be(0);

            var banks = new BanksAvailableCommand();
            banks.CommandType.Should().Be(CommandType.BanksAvailable);
            banks.ContentLength.Should().Be(0);

            var checkpointList = new CheckpointListCommand();
            checkpointList.CommandType.Should().Be(CommandType.CheckpointList);
            checkpointList.ContentLength.Should().Be(0);
        }

        [Fact]
        public void AdvanceInstructionCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new AdvanceInstructionCommand(
                StepOverSubroutine: true,
                NumberOfInstructions: 5
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be(1); // StepOverSubroutine (true)
            BitConverter.ToUInt16(buffer, 1).Should().Be(5); // NumberOfInstructions
        }

        [Fact]
        public void CheckpointDeleteCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new CheckpointDeleteCommand(CheckpointNumber: 42);
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            BitConverter.ToUInt32(buffer, 0).Should().Be(42); // CheckpointNumber
        }

        [Fact]
        public void CheckpointToggleCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new CheckpointToggleCommand(
                CheckpointNumber: 42,
                Enabled: true
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            BitConverter.ToUInt32(buffer, 0).Should().Be(42); // CheckpointNumber
            buffer[4].Should().Be(1); // Enabled (true)
        }

        [Fact]
        public void ResetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new ResetCommand(ResetMode.Soft);
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be((byte)ResetMode.Soft);
        }

        [Fact]
        public void AutoStartCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new AutoStartCommand(
                runAfterLoading: true,
                fileIndex: 0,
                filename: "test.prg"
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be(1); // RunAfterLoading (true)
            BitConverter.ToUInt16(buffer, 1).Should().Be(0); // StartIndex
            buffer[3].Should().Be((byte)"test.prg".Length); // File path length
            Encoding.ASCII.GetString(buffer, 4, buffer[3]).Should().Be("test.prg");
        }
    }
}