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

public class ViceToolsExtendedTests
{
    private readonly Mock<IViceBridge> _viceBridgeMock;
    private readonly ViceTools _viceTools;
    private readonly ViceConfiguration _config;

    public ViceToolsExtendedTests()
    {
        _viceBridgeMock = new Mock<IViceBridge>();
        _config = new ViceConfiguration();
        _viceTools = new ViceTools(_viceBridgeMock.Object, _config);
    }

    #region SetRegister Tests

    [Fact]
    public async Task SetRegister_Should_Set_Register_Successfully()
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
        var result = await _viceTools.SetRegister("A", "FF");

        // Assert
        result.Should().Be("Set A to $00FF");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<RegistersSetCommand>(cmd => 
                cmd.Items.Length == 1 &&
                cmd.Items[0].RegisterId == 0 &&
                cmd.Items[0].RegisterValue == 0xFF),
            true), Times.Once);
    }

    [Fact]
    public async Task SetRegister_Should_Handle_Unknown_Register()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _viceTools.SetRegister("Z", "FF"));
    }

    [Fact]
    public async Task SetRegister_Should_Handle_Invalid_Hex_Value()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => _viceTools.SetRegister("A", "GGGG"));
    }

    [Fact]
    public async Task SetRegister_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<RegistersResponse>(ErrorCode.InvalidParameterValue);
        
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.SetRegister("A", "FF"));
        ex.Message.Should().Contain("Failed to set register");
    }

    #endregion

    #region ContinueExecution Tests

    [Fact]
    public async Task ContinueExecution_Should_Resume_Successfully()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<ExitCommand>(), It.IsAny<bool>()))
            .Callback((ExitCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((ExitCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.ContinueExecution();

        // Assert
        result.Should().Be("Execution resumed");
    }

    [Fact]
    public async Task ContinueExecution_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(ErrorCode.GeneralFailure);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<ExitCommand>(), It.IsAny<bool>()))
            .Callback((ExitCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((ExitCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.ContinueExecution());
        ex.Message.Should().Contain("Failed to continue execution");
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task Reset_Should_Perform_Soft_Reset_Successfully()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<ResetCommand>(), It.IsAny<bool>()))
            .Callback((ResetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((ResetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.Reset("soft");

        // Assert
        result.Should().Be("Machine reset (soft)");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<ResetCommand>(cmd => cmd.Mode == ResetMode.Soft),
            false), Times.Once);
    }

    [Fact]
    public async Task Reset_Should_Perform_Hard_Reset_Successfully()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<ResetCommand>(), It.IsAny<bool>()))
            .Callback((ResetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((ResetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.Reset("hard");

        // Assert
        result.Should().Be("Machine reset (hard)");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<ResetCommand>(cmd => cmd.Mode == ResetMode.Hard),
            false), Times.Once);
    }

    [Fact]
    public async Task Reset_Should_Handle_Invalid_Mode()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _viceTools.Reset("invalid"));
    }

    #endregion

    #region GetInfo Tests

    [Fact]
    public async Task GetInfo_Should_Return_Version_Information()
    {
        // Arrange
        var infoResponse = new InfoResponse(0x02, ErrorCode.OK, 3, 8, 0, 0, 1234);
        var commandResponse = new CommandResponse<InfoResponse>(infoResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<InfoCommand>(), It.IsAny<bool>()))
            .Callback((InfoCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<InfoResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<InfoResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((InfoCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.GetInfo();

        // Assert
        result.Should().Be("VICE Version: 3.8.0.0\nSVN: 1234");
    }

    [Fact]
    public async Task GetInfo_Should_Handle_Null_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<InfoResponse>(ErrorCode.ObjectDoesNotExist);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<InfoCommand>(), It.IsAny<bool>()))
            .Callback((InfoCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<InfoResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<InfoResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((InfoCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.GetInfo());
        ex.Message.Should().Contain("Failed to get info");
    }

    #endregion

    #region GetBanks Tests

    [Fact]
    public async Task GetBanks_Should_Return_Bank_List()
    {
        // Arrange
        var banks = ImmutableArray.Create(
            new BankItem(0, "RAM"),
            new BankItem(1, "ROM"),
            new BankItem(2, "IO")
        );
        var banksResponse = new BanksAvailableResponse(0x02, ErrorCode.OK, banks);
        var commandResponse = new CommandResponse<BanksAvailableResponse>(banksResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<BanksAvailableCommand>(), It.IsAny<bool>()))
            .Callback((BanksAvailableCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<BanksAvailableResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<BanksAvailableResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((BanksAvailableCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.GetBanks();

        // Assert
        result.Should().Contain("Bank 0: RAM");
        result.Should().Contain("Bank 1: ROM");
        result.Should().Contain("Bank 2: IO");
    }

    [Fact]
    public async Task GetBanks_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<BanksAvailableResponse>(ErrorCode.GeneralFailure);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<BanksAvailableCommand>(), It.IsAny<bool>()))
            .Callback((BanksAvailableCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<BanksAvailableResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<BanksAvailableResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((BanksAvailableCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.GetBanks());
        ex.Message.Should().Contain("Failed to get banks");
    }

    #endregion
}