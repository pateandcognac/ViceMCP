using System.Text;
using FluentAssertions;
using ViceMCP.ViceBridge;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Shared;
using Xunit;

namespace ViceMCP.Tests
{
    public class ViceCommandTests2
    {
        [Fact]
        public void CheckpointGetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new CheckpointGetCommand(CheckpointNumber: 42);
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            BitConverter.ToUInt32(buffer, 0).Should().Be(42);
        }

        [Fact]
        public void ConditionSetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new ConditionSetCommand(
                checkpointNumber: 42,
                conditionExpression: "A == 0xFF"
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            BitConverter.ToUInt32(buffer, 0).Should().Be(42);
            buffer[4].Should().Be((byte)"A == 0xFF".Length);
            Encoding.ASCII.GetString(buffer, 5, buffer[4]).Should().Be("A == 0xFF");
        }

        [Fact]
        public void DumpCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new DumpCommand(
                saveRom: true,
                saveDisks: false,
                filename: "dump.vsf"
            );
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be(1); // SaveRom (true)
            buffer[1].Should().Be(0); // SaveDisks (false)
            buffer[2].Should().Be((byte)"dump.vsf".Length);
            Encoding.ASCII.GetString(buffer, 3, buffer[2]).Should().Be("dump.vsf");
        }

        [Fact]
        public void ExecuteUntilReturnCommand_Should_Have_Zero_Content_Length()
        {
            // Arrange
            var command = new ExecuteUntilReturnCommand();

            // Assert
            command.CommandType.Should().Be(CommandType.ExecuteUntilReturn);
            command.ContentLength.Should().Be(0);
        }

        [Fact]
        public void RegistersAvailableCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new RegistersAvailableCommand(MemSpace.MainMemory);
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be((byte)MemSpace.MainMemory);
        }

        [Fact]
        public void ResourceGetCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new ResourceGetCommand("TestResource");
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be((byte)"TestResource".Length);
            Encoding.ASCII.GetString(buffer, 1, buffer[0]).Should().Be("TestResource");
        }

        [Fact]
        public void UndumpCommand_Should_Write_Correct_Content()
        {
            // Arrange
            var command = new UndumpCommand("snapshot.vsf");
            var buffer = new byte[command.ContentLength];

            // Act
            command.WriteContent(buffer);

            // Assert
            buffer[0].Should().Be((byte)"snapshot.vsf".Length);
            Encoding.ASCII.GetString(buffer, 1, buffer[0]).Should().Be("snapshot.vsf");
        }

        [Fact]
        public void Commands_Should_Have_Correct_Types()
        {
            // Arrange & Act & Assert
            new CheckpointGetCommand(1).CommandType.Should().Be(CommandType.CheckpointGet);
            new ConditionSetCommand(1, "A==0").CommandType.Should().Be(CommandType.ConditionSet);
            new DumpCommand(true, true, "dump").CommandType.Should().Be(CommandType.Dump);
            new ExecuteUntilReturnCommand().CommandType.Should().Be(CommandType.ExecuteUntilReturn);
            new RegistersAvailableCommand(MemSpace.MainMemory).CommandType.Should().Be(CommandType.RegistersAvailable);
            new ResourceGetCommand("test").CommandType.Should().Be(CommandType.ResourceGet);
            new UndumpCommand("test").CommandType.Should().Be(CommandType.Undump);
        }

        [Fact]
        public void ParameterlessCommand_Should_Have_Zero_Content_Length()
        {
            // Test through concrete implementations
            new PingCommand().ContentLength.Should().Be(0);
            new ExitCommand().ContentLength.Should().Be(0);
            new QuitCommand().ContentLength.Should().Be(0);
            new InfoCommand().ContentLength.Should().Be(0);
            new BanksAvailableCommand().ContentLength.Should().Be(0);
            new CheckpointListCommand().ContentLength.Should().Be(0);
            new ExecuteUntilReturnCommand().ContentLength.Should().Be(0);
        }
    }
}