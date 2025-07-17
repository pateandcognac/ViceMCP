using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Moq;
using ViceMCP.ViceBridge;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Services.Abstract;
using ViceMCP.ViceBridge.Shared;

namespace ViceMCP.Tests;

public class ViceToolsAddressParsingTests
{
    private readonly Mock<IViceBridge> _viceBridgeMock;
    private readonly ViceTools _viceTools;
    private readonly ViceConfiguration _config;

    public ViceToolsAddressParsingTests()
    {
        _viceBridgeMock = new Mock<IViceBridge>();
        _config = new ViceConfiguration();
        _viceTools = new ViceTools(_viceBridgeMock.Object, _config);
    }

    [Fact]
    public async Task ReadMemory_Should_Handle_0x_Prefix()
    {
        // Arrange
        var expectedBytes = new byte[] { 0xAA, 0xBB };
        var buffer = BufferManager.GetBuffer((uint)expectedBytes.Length);
        // Clear the buffer first to ensure no residual data
        Array.Clear(buffer.Data, 0, buffer.Data.Length);
        Array.Copy(expectedBytes, buffer.Data, expectedBytes.Length);
        
        var memoryResponse = new MemoryGetResponse(0x02, ErrorCode.OK, buffer);
        var commandResponse = new CommandResponse<MemoryGetResponse>(memoryResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<MemoryGetCommand>(), It.IsAny<bool>()))
            .Callback((MemoryGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<MemoryGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<MemoryGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((MemoryGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.ReadMemory("0xC000", "0xC001");

        // Assert
        result.Should().Be("AA-BB");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<MemoryGetCommand>(cmd => 
                cmd.StartAddress == 0xC000 && 
                cmd.EndAddress == 0xC001),
            false), Times.Once);
    }

    [Fact]
    public async Task WriteMemory_Should_Handle_0x_Prefix()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<MemorySetCommand>(), It.IsAny<bool>()))
            .Callback((MemorySetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((MemorySetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.WriteMemory("0xD000", "AA BB CC");

        // Assert
        result.Should().Be("Wrote 3 bytes to $D000");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<MemorySetCommand>(cmd => cmd.StartAddress == 0xD000),
            true), Times.Once);
    }

    [Fact]
    public async Task SetRegister_Should_Handle_0x_Prefix()
    {
        // Arrange
        var registersResponse = new RegistersResponse(0x02, ErrorCode.OK, ImmutableArray<RegisterItem>.Empty);
        var commandResponse = new CommandResponse<RegistersResponse>(registersResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<RegistersSetCommand>(), It.IsAny<bool>()))
            .Callback((RegistersSetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<RegistersResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<RegistersResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((RegistersSetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.SetRegister("PC", "0xC000");

        // Assert
        result.Should().Be("Set PC to $C000");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<RegistersSetCommand>(cmd => 
                cmd.Items.Length == 1 &&
                cmd.Items[0].RegisterValue == 0xC000),
            true), Times.Once);
    }

    [Fact]
    public async Task SetCheckpoint_Should_Handle_0x_Prefix()
    {
        // Arrange
        var checkpointResponse = new CheckpointInfoResponse(0x02, ErrorCode.OK, 1, false, 0xE000, 0xE100, true, true, CpuOperation.Exec, false, 0, 0, false);
        var commandResponse = new CommandResponse<CheckpointInfoResponse>(checkpointResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointSetCommand>(), It.IsAny<bool>()))
            .Callback((CheckpointSetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<CheckpointInfoResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<CheckpointInfoResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointSetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.SetCheckpoint("0xE000", "0xE100");

        // Assert
        result.Should().Be("Checkpoint 1 set at $E000-$E100");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<CheckpointSetCommand>(cmd => 
                cmd.StartAddress == 0xE000 &&
                cmd.EndAddress == 0xE100),
            false), Times.Once);
    }

    [Theory]
    [InlineData("c000", 0xC000)]
    [InlineData("C000", 0xC000)]
    [InlineData("0xc000", 0xC000)]
    [InlineData("0XC000", 0xC000)]
    [InlineData("0xff", 0xFF)]
    [InlineData("00", 0x00)]
    public async Task ReadMemory_Should_Parse_Various_Hex_Formats(string input, ushort expected)
    {
        // Arrange
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<MemoryGetCommand>(), It.IsAny<bool>()))
            .Callback((MemoryGetCommand cmd, bool resumeOnStopped) => 
            {
                // Create response with fresh buffer to avoid pollution
                var buffer = BufferManager.GetBuffer(1);
                Array.Clear(buffer.Data, 0, buffer.Data.Length);
                buffer.Data[0] = 0x42;
                
                var memoryResponse = new MemoryGetResponse(0x02, ErrorCode.OK, buffer);
                var commandResponse = new CommandResponse<MemoryGetResponse>(memoryResponse);
                
                var commandType = typeof(ViceCommand<MemoryGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<MemoryGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((MemoryGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        await _viceTools.ReadMemory(input, input);

        // Assert
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<MemoryGetCommand>(cmd => 
                cmd.StartAddress == expected && 
                cmd.EndAddress == expected),
            false), Times.Once);
    }
}