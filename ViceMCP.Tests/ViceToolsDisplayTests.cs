using System.Reflection;
using FluentAssertions;
using Moq;
using ViceMCP.ViceBridge;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Services.Abstract;

namespace ViceMCP.Tests;

public class ViceToolsDisplayTests
{
    private readonly Mock<IViceBridge> _viceBridgeMock;
    private readonly ViceTools _viceTools;
    private readonly ViceConfiguration _config;

    public ViceToolsDisplayTests()
    {
        _viceBridgeMock = new Mock<IViceBridge>();
        _config = new ViceConfiguration();
        _viceTools = new ViceTools(_viceBridgeMock.Object, _config);
    }

    #region GetDisplay Tests

    [Fact]
    public async Task GetDisplay_Should_Return_Display_Info()
    {
        // Arrange
        var imageBuffer = BufferManager.GetBuffer(1024);
        Array.Clear(imageBuffer.Data, 0, imageBuffer.Data.Length);
        var displayResponse = new DisplayGetResponse(0x02, ErrorCode.OK, 320, 200, 0, 0, 320, 200, 8, imageBuffer);
        var commandResponse = new CommandResponse<DisplayGetResponse>(displayResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<DisplayGetCommand>(), false))
            .Callback((DisplayGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<DisplayGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<DisplayGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((DisplayGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.GetDisplay();

        // Assert
        result.Should().Contain("Display captured: 320x200 (8 bpp)");
        result.Should().Contain("Image data: 1024 bytes");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<DisplayGetCommand>(cmd => 
                cmd.UseVic == true &&
                cmd.Format == ImageFormat.Indexed),
            false), Times.Once);
        
        imageBuffer.Dispose();
    }

    [Fact]
    public async Task GetDisplay_Should_Use_VICII_When_UseVic_False()
    {
        // Arrange
        var imageBuffer = BufferManager.GetBuffer(2048);
        Array.Clear(imageBuffer.Data, 0, imageBuffer.Data.Length);
        var displayResponse = new DisplayGetResponse(0x02, ErrorCode.OK, 320, 200, 0, 0, 320, 200, 8, imageBuffer);
        var commandResponse = new CommandResponse<DisplayGetResponse>(displayResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<DisplayGetCommand>(), false))
            .Callback((DisplayGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<DisplayGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<DisplayGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((DisplayGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.GetDisplay(false);

        // Assert
        _viceBridgeMock.Verify(x => x.EnqueueCommand(
            It.Is<DisplayGetCommand>(cmd => cmd.UseVic == false),
            false), Times.Once);
        
        imageBuffer.Dispose();
    }

    [Fact]
    public async Task GetDisplay_Should_Handle_No_Image_Data()
    {
        // Arrange
        var displayResponse = new DisplayGetResponse(0x02, ErrorCode.OK, 320, 200, 0, 0, 320, 200, 8, null);
        var commandResponse = new CommandResponse<DisplayGetResponse>(displayResponse);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<DisplayGetCommand>(), false))
            .Callback((DisplayGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<DisplayGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<DisplayGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((DisplayGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.GetDisplay());
        ex.Message.Should().Be("No image data returned");
    }

    [Fact]
    public async Task GetDisplay_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<DisplayGetResponse>(ErrorCode.GeneralFailure);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<DisplayGetCommand>(), false))
            .Callback((DisplayGetCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<DisplayGetResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<DisplayGetResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((DisplayGetCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.GetDisplay());
        ex.Message.Should().Contain("Failed to get display");
    }

    #endregion

    #region QuitVice Tests

    [Fact]
    public async Task QuitVice_Should_Quit_Successfully()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(new EmptyViceResponse(0x02, ErrorCode.OK));
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<QuitCommand>(), false))
            .Callback((QuitCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((QuitCommand cmd, bool resumeOnStopped) => cmd);

        // Act
        var result = await _viceTools.QuitVice();

        // Assert
        result.Should().Be("VICE emulator quit");
        _viceBridgeMock.Verify(x => x.EnqueueCommand(It.IsAny<QuitCommand>(), false), Times.Once);
    }

    [Fact]
    public async Task QuitVice_Should_Handle_Error_Response()
    {
        // Arrange
        var commandResponse = new CommandResponse<EmptyViceResponse>(ErrorCode.GeneralFailure);
        
        _viceBridgeMock.Setup(x => x.Start(6502));
        
        _viceBridgeMock
            .Setup(x => x.EnqueueCommand(It.IsAny<QuitCommand>(), false))
            .Callback((QuitCommand cmd, bool resumeOnStopped) => 
            {
                var commandType = typeof(ViceCommand<EmptyViceResponse>);
                var tcsField = commandType.GetField("tcs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var tcs = (TaskCompletionSource<CommandResponse<EmptyViceResponse>>)tcsField!.GetValue(cmd)!;
                tcs.SetResult(commandResponse);
            })
            .Returns((QuitCommand cmd, bool resumeOnStopped) => cmd);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _viceTools.QuitVice());
        ex.Message.Should().Contain("Failed to quit VICE");
    }

    #endregion
}