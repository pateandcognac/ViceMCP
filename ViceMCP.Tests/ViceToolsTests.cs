using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Moq;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using Righthand.ViceMonitor.Bridge.Shared;

namespace ViceMCP.Tests;

public class ViceToolsTests
{
    private readonly Mock<IViceBridge> _viceBridgeMock;
    private readonly ViceTools _viceTools;
    private readonly ViceConfiguration _config;

    public ViceToolsTests()
    {
        _viceBridgeMock = new Mock<IViceBridge>();
        _config = new ViceConfiguration();
        _viceTools = new ViceTools(_viceBridgeMock.Object, _config);
    }

    [Fact]
    public async Task ReadMemory_Should_Return_Formatted_Bytes()
    {
        // Arrange
        var expectedBytes = new byte[] { 0x08, 0x05, 0x0C, 0x0C, 0x0F };
        var buffer = BufferManager.GetBuffer((uint)expectedBytes.Length);
        Array.Clear(buffer.Data, 0, buffer.Data.Length);
        Array.Copy(expectedBytes, buffer.Data, expectedBytes.Length);
        
        var memoryResponse = new MemoryGetResponse(0x02, ErrorCode.OK, buffer);
        var commandResponse = new CommandResponse<MemoryGetResponse>(memoryResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<MemoryGetCommand>(), false))
            .Callback((MemoryGetCommand cmd, bool resumeOnStopped) => 
            {
                // Use reflection to set the response
                var commandType = typeof(ViceCommand<MemoryGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<MemoryGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((MemoryGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.ReadMemory("0400", "0404");

        // Assert
        result.Should().Be("08-05-0C-0C-0F");
        // Note: Start() may or may not be called depending on static state
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<MemoryGetCommand>(cmd => 
                cmd.SideEffects == 0 &&
                cmd.StartAddress == 0x0400 && 
                cmd.EndAddress == 0x0404 &&
                cmd.MemSpace == MemSpace.MainMemory &&
                cmd.BankId == 0),
            false), Times.Once);
        
        buffer.Dispose();
    }

    [Fact]
    public async Task ReadMemory_Should_Handle_Parse_Errors()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => _viceTools.ReadMemory("GGGG", "0404"));
    }

    [Fact]
    public async Task WriteMemory_Should_Write_Bytes_Successfully()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<MemorySetCommand>(), false))
            .Callback((MemorySetCommand cmd, bool resumeOnStopped) => 
            {
                // Use reflection to set the response
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((MemorySetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.WriteMemory("0400", "08 05 0C 0C 0F");

        // Assert
        result.Should().Be("Wrote 5 bytes to $0400");
        // Note: Start() may or may not be called depending on static state
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<MemorySetCommand>(cmd => 
                cmd.SideEffects == 0 &&
                cmd.StartAddress == 0x0400 &&
                cmd.MemSpace == MemSpace.MainMemory &&
                cmd.BankId == 0),
            false), Times.Once);
    }

    [Fact]
    public async Task WriteMemory_Should_Handle_Invalid_Data_Format()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => _viceTools.WriteMemory("0400", "GG ZZ"));
    }
    
    [Fact]
    public async Task GetRegisters_Should_Return_Formatted_Register_Values()
    {
        // Arrange
        var items = ImmutableArray.Create(
            new RegisterItem(0, 0x42),
            new RegisterItem(1, 0x10),
            new RegisterItem(3, 0xC000)
        );
        var registersResponse = new RegistersResponse(0x02, ErrorCode.OK, items);
        var commandResponse = new CommandResponse<RegistersResponse>(registersResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<RegistersGetCommand>(), false))
            .Callback((RegistersGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<RegistersResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<RegistersResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((RegistersGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.GetRegisters();

        // Assert
        result.Should().Contain("Register 0: $0042");
        result.Should().Contain("Register 1: $0010");
        result.Should().Contain("Register 3: $C000");
    }
    
    [Fact]
    public async Task Ping_Should_Return_Success_Message()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<PingCommand>(), false))
            .Callback((PingCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((PingCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.Ping();

        // Assert
        result.Should().Be("Pong! VICE is responding");
    }
    
    [Fact]
    public async Task Step_Should_Return_Success_Message()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<AdvanceInstructionCommand>(), false))
            .Callback((AdvanceInstructionCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((AdvanceInstructionCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.Step(3, true);

        // Assert
        result.Should().Be("Stepped 3 instruction(s)");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<AdvanceInstructionCommand>(cmd => 
                cmd.StepOverSubroutine == true &&
                cmd.NumberOfInstructions == 3),
            false), Times.Once);
    }
    
    [Fact]
    public async Task ReadMemory_Should_Handle_No_Memory_Data()
    {
        // Arrange
        var memoryResponse = new MemoryGetResponse(0x02, ErrorCode.OK, null);
        var commandResponse = new CommandResponse<MemoryGetResponse>(memoryResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<MemoryGetCommand>(), false))
            .Callback((MemoryGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<MemoryGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<MemoryGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((MemoryGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.ReadMemory("0400", "0404"));
        ex.Message.Should().Be("No memory data returned");
    }
    
    [Fact]
    public async Task ReadMemory_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<MemoryGetResponse>(ErrorCode.InvalidMemSpace);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<MemoryGetCommand>(), false))
            .Callback((MemoryGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<MemoryGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<MemoryGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((MemoryGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.ReadMemory("0400", "0404"));
        ex.Message.Should().Contain("Failed to read memory");
    }
    
    [Fact]
    public async Task WriteMemory_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(ErrorCode.InvalidMemSpace);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<MemorySetCommand>(), false))
            .Callback((MemorySetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((MemorySetCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.WriteMemory("0400", "08 05 0C 0C 0F"));
        ex.Message.Should().Contain("Failed to write memory");
    }
    
    [Fact]
    public async Task GetRegisters_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<RegistersResponse>(ErrorCode.GeneralFailure);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<RegistersGetCommand>(), false))
            .Callback((RegistersGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<RegistersResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<RegistersResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((RegistersGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.GetRegisters());
        ex.Message.Should().Contain("Failed to get registers");
    }
    
    [Fact]
    public async Task Ping_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(ErrorCode.GeneralFailure);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<PingCommand>(), false))
            .Callback((PingCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((PingCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.Ping());
        ex.Message.Should().Contain("Failed to ping");
    }
    
    [Fact]
    public async Task Step_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(ErrorCode.GeneralFailure);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<AdvanceInstructionCommand>(), false))
            .Callback((AdvanceInstructionCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((AdvanceInstructionCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.Step());
        ex.Message.Should().Contain("Failed to step");
    }
}