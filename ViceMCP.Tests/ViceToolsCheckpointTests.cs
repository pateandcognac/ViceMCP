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

public class ViceToolsCheckpointTests
{
    private readonly Mock<IViceBridge> _viceBridgeMock;
    private readonly ViceTools _viceTools;
    private readonly ViceConfiguration _config;

    public ViceToolsCheckpointTests()
    {
        _viceBridgeMock = new Mock<IViceBridge>();
        _config = new ViceConfiguration();
        _viceTools = new ViceTools(_viceBridgeMock.Object, _config);
    }

    #region SetCheckpoint Tests

    [Fact]
    public async Task SetCheckpoint_Should_Create_Checkpoint_Successfully()
    {
        // Arrange
        var checkpointResponse = new CheckpointInfoResponse(0x02, ErrorCode.OK, 1, false, 0xC000, 0xC000, true, true, CpuOperation.Exec, false, 0, 0, false);
        var commandResponse = new CommandResponse<CheckpointInfoResponse>(checkpointResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointSetCommand>(), false))
            .Callback((CheckpointSetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<CheckpointInfoResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<CheckpointInfoResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointSetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.SetCheckpoint("C000");

        // Assert
        result.Should().Be("Checkpoint 1 set at $C000-$C000");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<CheckpointSetCommand>(cmd => 
                cmd.StartAddress == 0xC000 &&
                cmd.EndAddress == 0xC000 &&
                cmd.StopWhenHit == true &&
                cmd.Enabled == true &&
                cmd.CpuOperation == CpuOperation.Exec),
            false), Times.Once);
    }

    [Fact]
    public async Task SetCheckpoint_Should_Create_Range_Checkpoint()
    {
        // Arrange
        var checkpointResponse = new CheckpointInfoResponse(0x02, ErrorCode.OK, 2, false, 0xC000, 0xC100, true, true, CpuOperation.Exec, false, 0, 0, false);
        var commandResponse = new CommandResponse<CheckpointInfoResponse>(checkpointResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointSetCommand>(), false))
            .Callback((CheckpointSetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<CheckpointInfoResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<CheckpointInfoResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointSetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.SetCheckpoint("C000", "C100", false, false);

        // Assert
        result.Should().Be("Checkpoint 2 set at $C000-$C100");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<CheckpointSetCommand>(cmd => 
                cmd.StartAddress == 0xC000 &&
                cmd.EndAddress == 0xC100 &&
                cmd.StopWhenHit == false &&
                cmd.Enabled == false),
            false), Times.Once);
    }

    [Fact]
    public async Task SetCheckpoint_Should_Handle_Invalid_Address()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => _viceTools.SetCheckpoint("ZZZZ"));
    }

    [Fact]
    public async Task SetCheckpoint_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<CheckpointInfoResponse>(ErrorCode.InvalidParameterValue);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointSetCommand>(), false))
            .Callback((CheckpointSetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<CheckpointInfoResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<CheckpointInfoResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointSetCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.SetCheckpoint("C000"));
        ex.Message.Should().Contain("Failed to set checkpoint");
    }

    #endregion

    #region ListCheckpoints Tests

    [Fact]
    public async Task ListCheckpoints_Should_Return_Empty_When_No_Checkpoints()
    {
        // Arrange
        var checkpointsResponse = new CheckpointListResponse(0x02, ErrorCode.OK, 0, ImmutableArray<CheckpointInfoResponse>.Empty);
        var commandResponse = new CommandResponse<CheckpointListResponse>(checkpointsResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointListCommand>(), false))
            .Callback((CheckpointListCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<CheckpointListResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<CheckpointListResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointListCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.ListCheckpoints();

        // Assert
        result.Should().Be("No checkpoints set");
    }

    [Fact]
    public async Task ListCheckpoints_Should_Return_Checkpoint_List()
    {
        // Arrange
        var checkpoints = ImmutableArray.Create(
            new CheckpointInfoResponse(0x02, ErrorCode.OK, 1, false, 0xC000, 0xC000, true, true, CpuOperation.Exec, false, 5, 0, false),
            new CheckpointInfoResponse(0x02, ErrorCode.OK, 2, true, 0xD000, 0xD000, true, false, CpuOperation.Store, false, 10, 2, false)
        );
        var checkpointsResponse = new CheckpointListResponse(0x02, ErrorCode.OK, 2, checkpoints);
        var commandResponse = new CommandResponse<CheckpointListResponse>(checkpointsResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointListCommand>(), false))
            .Callback((CheckpointListCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<CheckpointListResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<CheckpointListResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointListCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.ListCheckpoints();

        // Assert
        result.Should().Contain("#1: $C000-$C000 (enabled) Hits: 5");
        result.Should().Contain("#2: $D000-$D000 (disabled) Hits: 10 [HIT]");
    }

    #endregion

    #region DeleteCheckpoint Tests

    [Fact]
    public async Task DeleteCheckpoint_Should_Delete_Successfully()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointDeleteCommand>(), false))
            .Callback((CheckpointDeleteCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointDeleteCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.DeleteCheckpoint(1);

        // Assert
        result.Should().Be("Deleted checkpoint #1");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<CheckpointDeleteCommand>(cmd => cmd.CheckpointNumber == 1),
            false), Times.Once);
    }

    [Fact]
    public async Task DeleteCheckpoint_Should_Handle_Invalid_Checkpoint()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(ErrorCode.ObjectDoesNotExist);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointDeleteCommand>(), false))
            .Callback((CheckpointDeleteCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointDeleteCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.DeleteCheckpoint(999));
        ex.Message.Should().Contain("Failed to delete checkpoint");
    }

    #endregion

    #region ToggleCheckpoint Tests

    [Fact]
    public async Task ToggleCheckpoint_Should_Enable_Successfully()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointToggleCommand>(), false))
            .Callback((CheckpointToggleCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointToggleCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.ToggleCheckpoint(1, true);

        // Assert
        result.Should().Be("Checkpoint #1 enabled");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<CheckpointToggleCommand>(cmd => 
                cmd.CheckpointNumber == 1 &&
                cmd.Enabled == true),
            false), Times.Once);
    }

    [Fact]
    public async Task ToggleCheckpoint_Should_Disable_Successfully()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<CheckpointToggleCommand>(), false))
            .Callback((CheckpointToggleCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((CheckpointToggleCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.ToggleCheckpoint(1, false);

        // Assert
        result.Should().Be("Checkpoint #1 disabled");
    }

    #endregion
}