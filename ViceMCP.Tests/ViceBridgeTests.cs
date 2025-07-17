using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ViceMCP.ViceBridge;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Exceptions;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Services.Abstract;
using ViceMCP.ViceBridge.Services.Implementation;
using Xunit;

namespace ViceMCP.Tests
{
    public class ViceBridgeTests : IDisposable
    {
        private readonly Mock<ILogger<ViceBridge.Services.Implementation.ViceBridge>> _loggerMock;
        private readonly Mock<ILogger<ResponseBuilder>> _responseLoggerMock;
        private readonly Mock<IPerformanceProfiler> _performanceProfilerMock;
        private readonly Mock<IMessagesHistory> _messagesHistoryMock;
        private readonly ResponseBuilder _responseBuilder;
        private readonly ViceBridge.Services.Implementation.ViceBridge _viceBridge;
        private TcpListener? _testListener;

        public ViceBridgeTests()
        {
            _loggerMock = new Mock<ILogger<ViceBridge.Services.Implementation.ViceBridge>>();
            _responseLoggerMock = new Mock<ILogger<ResponseBuilder>>();
            _performanceProfilerMock = new Mock<IPerformanceProfiler>();
            _messagesHistoryMock = new Mock<IMessagesHistory>();
            _responseBuilder = new ResponseBuilder(_responseLoggerMock.Object);
            
            _viceBridge = new ViceBridge.Services.Implementation.ViceBridge(
                _loggerMock.Object,
                _responseBuilder,
                _performanceProfilerMock.Object,
                _messagesHistoryMock.Object);
        }

        public void Dispose()
        {
            _viceBridge?.Dispose();
            _testListener?.Stop();
        }

        [Fact]
        public void Constructor_Should_Initialize_Properties()
        {
            // Assert
            _viceBridge.PerformanceProfiler.Should().Be(_performanceProfilerMock.Object);
            _viceBridge.MessagesHistory.Should().Be(_messagesHistoryMock.Object);
            _viceBridge.IsStarted.Should().BeFalse();
            _viceBridge.IsConnected.Should().BeFalse();
            _viceBridge.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Start_Should_Set_IsStarted_To_True()
        {
            // Act
            _viceBridge.Start(6502);

            // Assert
            _viceBridge.IsStarted.Should().BeTrue();
        }

        [Fact]
        public void Start_When_Already_Started_Should_Log_Warning()
        {
            // Arrange
            _viceBridge.Start(6502);

            // Act
            _viceBridge.Start(6502);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Already started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task StopAsync_When_Not_Started_Should_Return_Immediately()
        {
            // Act
            await _viceBridge.StopAsync(false);

            // Assert - should not throw
            _viceBridge.IsStarted.Should().BeFalse();
        }

        [Fact]
        public async Task StopAsync_Should_Stop_Running_Instance()
        {
            // Arrange
            _viceBridge.Start(6502);

            // Act
            await _viceBridge.StopAsync(false);

            // Assert
            _viceBridge.IsStarted.Should().BeFalse();
        }

        [Fact]
        public void EnqueueCommand_When_Not_Started_Should_Throw()
        {
            // Arrange
            var command = new PingCommand();

            // Act
            var act = () => _viceBridge.EnqueueCommand(command);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Bridge is not started");
        }

        [Fact]
        public void EnqueueCommand_With_Valid_Command_Should_Queue()
        {
            // Arrange
            _viceBridge.Start(6502);
            var buffer = BufferManager.GetBuffer(1);
            buffer.Data[0] = 0;
            var command = new MemorySetCommand(0, 0x1000, MemSpace.MainMemory, 0, buffer);

            // Act
            var result = _viceBridge.EnqueueCommand(command);

            // Assert
            result.Should().BeSameAs(command);
        }

        [Fact]
        public void EnqueueCommand_With_Valid_Command_Should_Return_Command()
        {
            // Arrange
            _viceBridge.Start(6502);
            var command = new PingCommand();

            // Act
            var result = _viceBridge.EnqueueCommand(command);

            // Assert
            result.Should().BeSameAs(command);
        }

        [Fact]
        public async Task Connection_Should_Raise_ConnectedChanged_Events()
        {
            // Arrange
            var connectedEvents = new List<bool>();
            _viceBridge.ConnectedChanged += (_, e) => connectedEvents.Add(e.IsConnected);

            // Start a test TCP listener
            _testListener = new TcpListener(IPAddress.Loopback, 6503);
            _testListener.Start();

            // Act
            _viceBridge.Start(6503);
            
            // Wait a bit for connection attempt
            await Task.Delay(100);

            await _viceBridge.StopAsync(false);

            // Assert
            connectedEvents.Should().Contain(true); // Connected event
            connectedEvents.Should().Contain(false); // Disconnected event
        }

        [Fact]
        public async Task WaitForConnectionStatusChangeAsync_Should_Complete_On_Status_Change()
        {
            // Arrange
            _testListener = new TcpListener(IPAddress.Loopback, 6504);
            _testListener.Start();

            // Act
            _viceBridge.Start(6504);
            var waitTask = _viceBridge.WaitForConnectionStatusChangeAsync();
            
            // Wait for connection
            await Task.Delay(100);
            
            var result = await waitTask;

            // Assert
            result.Should().BeTrue(); // Connected
        }

        [Fact]
        public async Task WaitForConnectionStatusChangeAsync_Should_Cancel_With_Token()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var waitTask = _viceBridge.WaitForConnectionStatusChangeAsync(cts.Token);
            cts.Cancel();

            // Assert
            var act = async () => await waitTask;
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task DisposeAsync_Should_Stop_Bridge()
        {
            // Arrange
            _viceBridge.Start(6502);

            // Act
            await _viceBridge.DisposeAsync();

            // Assert
            _viceBridge.IsStarted.Should().BeFalse();
        }
    }
}