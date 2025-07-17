using FluentAssertions;
using ViceMCP.ViceBridge;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Shared;
using Xunit;

namespace ViceMCP.Tests
{
    public class ViceBridgeSimpleTests
    {
        [Fact]
        public void EmptyViceResponse_Should_Be_Created()
        {
            // Arrange & Act
            var response = new EmptyViceResponse(2, ErrorCode.OK);

            // Assert
            response.Should().NotBeNull();
            response.ErrorCode.Should().Be(ErrorCode.OK);
            response.ApiVersion.Should().Be(2);
        }

        [Fact]
        public void BankItem_Should_Be_Created()
        {
            // Arrange & Act
            var bank = new BankItem(1, "RAM");

            // Assert
            bank.Should().NotBeNull();
            bank.BankId.Should().Be(1);
            bank.Name.Should().Be("RAM");
        }

        [Fact]
        public void Commands_Should_Have_Correct_CommandType()
        {
            // Arrange & Act & Assert
            new PingCommand().CommandType.Should().Be(CommandType.Ping);
            new ExitCommand().CommandType.Should().Be(CommandType.Exit);
            new InfoCommand().CommandType.Should().Be(CommandType.Info);
            new QuitCommand().CommandType.Should().Be(CommandType.Quit);
            new BanksAvailableCommand().CommandType.Should().Be(CommandType.BanksAvailable);
            new CheckpointListCommand().CommandType.Should().Be(CommandType.CheckpointList);
            new RegistersGetCommand(MemSpace.MainMemory).CommandType.Should().Be(CommandType.RegistersGet);
        }

        [Fact]
        public void MemoryGetCommand_Should_Be_Created()
        {
            // Arrange & Act
            var command = new MemoryGetCommand(0, 0x1000, 0x10FF, MemSpace.MainMemory, 0);

            // Assert
            command.StartAddress.Should().Be(0x1000);
            command.EndAddress.Should().Be(0x10FF);
            command.MemSpace.Should().Be(MemSpace.MainMemory);
            command.CollectErrors().Should().BeEmpty();
        }

        [Fact]
        public void BufferManager_Should_Return_Buffers()
        {
            // Arrange & Act
            using var buffer1 = BufferManager.GetBuffer(100);
            using var buffer2 = BufferManager.GetBuffer(200);

            // Assert
            buffer1.Size.Should().Be(100);
            buffer1.Data.Should().NotBeNull();
            buffer1.Data.Length.Should().BeGreaterThanOrEqualTo(100);

            buffer2.Size.Should().Be(200);
            buffer2.Data.Should().NotBeNull();
            buffer2.Data.Length.Should().BeGreaterThanOrEqualTo(200);
        }

        [Fact]
        public void ManagedBuffer_Should_Store_Data()
        {
            // Arrange
            using var buffer = BufferManager.GetBuffer(10);

            // Act
            for (int i = 0; i < 10; i++)
            {
                buffer.Data[i] = (byte)i;
            }

            // Assert
            for (int i = 0; i < 10; i++)
            {
                buffer.Data[i].Should().Be((byte)i);
            }
        }

        [Fact]
        public void CommandResponse_Should_Indicate_Success()
        {
            // Arrange & Act
            var successResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(2, ErrorCode.OK));
            var errorResponse = new CommandResponse<EmptyViceResponse>(ErrorCode.ObjectDoesNotExist);

            // Assert
            successResponse.IsSuccess.Should().BeTrue();
            successResponse.ErrorCode.Should().Be(ErrorCode.OK);
            successResponse.Response.Should().NotBeNull();

            errorResponse.IsSuccess.Should().BeFalse();
            errorResponse.ErrorCode.Should().Be(ErrorCode.ObjectDoesNotExist);
            errorResponse.Response.Should().BeNull();
        }

        [Fact]
        public void RegisterItem_Should_Store_Values()
        {
            // Arrange & Act
            var register = new RegisterItem(0, 0xFF);

            // Assert
            register.RegisterId.Should().Be((byte)0);
            register.RegisterValue.Should().Be((ushort)0xFF);
        }
    }
}