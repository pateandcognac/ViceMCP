using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Exceptions;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Services.Abstract;
using System.Buffers;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ViceMCP.ViceBridge.Services.Implementation
{
    /// <summary>
    /// Facilitates communication with the VICE emulator using the binary monitor protocol.
    /// Provides mechanisms for managing connection state, sending commands, and receiving responses.
    /// </summary>
    public sealed class ViceBridge : IViceBridge
    {
        private readonly ILogger<ViceBridge> _logger;
        private readonly ResponseBuilder _responseBuilder;
        private readonly ArrayPool<byte> _byteArrayPool = ArrayPool<byte>.Shared;
        private readonly ConcurrentQueue<PendingCommand> _commandQueue = new();
        private readonly SemaphoreSlim _commandAvailable = new(0);

        private CancellationTokenSource? _connectionCts;
        private Task? _connectionTask;
        private Socket? _socket;
        private uint _currentRequestId;
        private bool _isConnected;

        public IPerformanceProfiler PerformanceProfiler { get; }
        public IMessagesHistory MessagesHistory { get; }

        public bool IsStarted => _connectionTask != null;
        public Task? RunnerTask => _connectionTask;

        public event EventHandler<ViceResponseEventArgs>? ViceResponse;
        public event EventHandler<ConnectedChangedEventArgs>? ConnectedChanged;

        /// <summary>
        /// Indicates whether the connection with the VICE server is currently established.
        /// </summary>
        /// <remarks>
        /// This property is set to true when a successful connection to the VICE server is
        /// established, and updates to false when the connection is lost or terminated. Any
        /// changes to the property's value trigger the <see cref="ConnectedChanged"/> event.
        /// </remarks>
        /// <value>
        /// A boolean value indicating the connection status. Returns true if connected,
        /// otherwise false.
        /// </value>
        public bool IsConnected
        {
            get { lock (this) { return _isConnected; } }
            private set
            {
                lock (this)
                {
                    if (_isConnected != value)
                    {
                        _isConnected = value;
                        ConnectedChanged?.Invoke(this, new ConnectedChangedEventArgs(value));
                    }
                }
            }
        }

        public bool IsRunning => IsConnected;

        public ViceBridge(ILogger<ViceBridge> logger, ResponseBuilder responseBuilder,
            IPerformanceProfiler performanceProfiler, IMessagesHistory messagesHistory)
        {
            _logger = logger;
            _responseBuilder = responseBuilder;
            PerformanceProfiler = performanceProfiler;
            MessagesHistory = messagesHistory;
        }

        /// <summary>
        /// Starts the connection process to the VICE emulator on the specified port.
        /// </summary>
        /// <param name="port">
        /// The port number to connect to. Defaults to 6502 if no port is specified.
        /// </param>
        public void Start(int port = 6502)
        {
            if (IsStarted)
            {
                _logger.LogWarning("Already started");
                return;
            }

            _connectionCts = new CancellationTokenSource();
            _connectionTask = Task.Run(() => ConnectionLoopAsync(port, _connectionCts.Token));
        }

        /// <summary>
        /// Stops the connection process to the VICE emulator asynchronously.
        /// </summary>
        /// <param name="waitForQueueToProcess">
        /// A boolean indicating whether to wait for the command queue to process before stopping.
        /// If set to true, the method signals no more commands will be added and waits for the queue
        /// to drain before stopping. If set to false, the connection is cancelled immediately.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// </returns>
        public async Task StopAsync(bool waitForQueueToProcess)
        {
            if (!IsStarted || _connectionCts == null) return;

            if (!waitForQueueToProcess)
            {
                // Cancel immediately
                await _connectionCts.CancelAsync();
            }
            else
            {
                // Signal no more commands will be added and wait for queue to drain
                _connectionCts.Cancel();
            }

            if (_connectionTask != null)
            {
                try { await _connectionTask; }
                catch (OperationCanceledException) { }
            }

            _connectionCts?.Dispose();
            _connectionCts = null;
            _connectionTask = null;
        }

        /// <summary>
        /// Enqueues a command for execution on the VICE bridge.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the command to enqueue, which must implement the <see cref="IViceCommand"/> interface.
        /// </typeparam>
        /// <param name="command">
        /// The command to be queued for execution. The command is validated before being enqueued.
        /// </param>
        /// <param name="resumeOnStopped">
        /// A boolean indicating whether the command should automatically resume processing if the VICE bridge is in a stopped state. Defaults to false.
        /// </param>
        /// <returns>
        /// The same command instance that was enqueued.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the bridge is not started when attempting to enqueue a command.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the provided command contains validation errors.
        /// </exception>
        public T EnqueueCommand<T>(T command, bool resumeOnStopped = false) where T : IViceCommand
        {
            if (!IsStarted)
                throw new InvalidOperationException("Bridge is not started");

            var errors = command.CollectErrors();
            if (errors.Length > 0)
                throw new ArgumentException(string.Join(Environment.NewLine, errors));

            _commandQueue.Enqueue(new PendingCommand(command, resumeOnStopped));
            _commandAvailable.Release();
            return command;
        }

        /// <summary>
        /// Maintains a connection loop to the VICE emulator, attempting to connect and process commands
        /// while handling errors and disconnections.
        /// </summary>
        /// <param name="port">
        /// The port number to connect to the VICE emulator.
        /// </param>
        /// <param name="ct">
        /// The cancellation token used to stop the connection loop.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation of the connection loop.
        /// </returns>
        private async Task ConnectionLoopAsync(int port, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync(port, ct);
                    await ProcessCommandsAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Connection error, will retry");
                    IsConnected = false;
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
                finally
                {
                    CloseSocket();
                }
            }
        }

        /// <summary>
        /// Establishes a connection to the VICE emulator on the specified port.
        /// </summary>
        /// <param name="port">
        /// The port number to connect to.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous connection operation.
        /// </returns>
        private async Task ConnectAsync(int port, CancellationToken ct)
        {
            // Wait for VICE to start listening
            await WaitForPort(port, ct);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await _socket.ConnectAsync("localhost", port, ct);

            _logger.LogInformation("Connected to VICE on port {Port}", port);
            IsConnected = true;
        }

        /// <summary>
        /// Continuously processes incoming commands and data within the established connection loop.
        /// Handles the execution of queued commands and monitors incoming data from the VICE emulator.
        /// The method operates until the cancellation token signals termination or the socket connection is closed.
        /// </summary>
        /// <param name="ct">
        /// A token to monitor for cancellation requests. If cancellation is requested, the method will gracefully exit.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation of processing commands and incoming data.
        /// </returns>
        private async Task ProcessCommandsAsync(CancellationToken ct)
        {
            var receiveBuffer = new byte[65536];
            var receiveTask = Task.CompletedTask;

            while (!ct.IsCancellationRequested && _socket?.Connected == true)
            {
                // Check for incoming data
                if (receiveTask.IsCompleted && _socket.Available > 0)
                {
                    receiveTask = ProcessIncomingDataAsync(ct);
                }

                // Process commands with timeout
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(100));

                    await _commandAvailable.WaitAsync(timeoutCts.Token);

                    if (_commandQueue.TryDequeue(out var pending))
                    {
                        await ProcessCommandAsync(pending, ct);
                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // Timeout is expected, just check for data and continue
                }
            }

            await receiveTask; // Wait for any pending receive operation
        }

        /// <summary>
        /// Processes an individual pending command by sending it to the VICE emulator,
        /// waiting for a response, and handling any necessary post-processing such as auto-resume behavior.
        /// </summary>
        /// <param name="pending">
        /// The pending command to be processed, including the command itself and whether auto-resume is enabled if the VICE emulator is stopped.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to propagate notification that the operation should be canceled.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation of processing the command.
        /// </returns>
        private async Task ProcessCommandAsync(PendingCommand pending, CancellationToken ct)
        {
            try
            {
                // Send command
                await SendCommandAsync(_socket!, _currentRequestId, pending.Command, ct);

                // Wait for response
                var response = await WaitForResponseAsync(_currentRequestId, ct);

                // Always check if VICE needs to be resumed after successful commands
                if (response.ErrorCode == ErrorCode.OK)
                {
                    // Skip for ExitCommand to avoid infinite loop
                    var commandType = pending.Command.GetType().Name;
                    if (commandType == "ExitCommand")
                    {
                        _logger.LogDebug("Skipping auto-resume check for ExitCommand");
                    }
                    else
                    {
                        // Wait at least one jiffy (17ms) to ensure VICE has settled
                        await Task.Delay(20, ct);
                        
                        // Check if VICE is paused using jiffy clock
                        var isPaused = await IsVicePausedAsync(ct);
                        
                        if (isPaused)
                        {
                            _logger.LogInformation("Auto-resume: VICE is paused after {CommandType}, sending exit command", 
                                commandType);
                            
                            var exitCommand = new ExitCommand();
                            _currentRequestId++;
                            await SendCommandAsync(_socket!, _currentRequestId, exitCommand, ct);
                            var exitResponse = await WaitForResponseAsync(_currentRequestId, ct);

                            _logger.LogInformation("Auto-resume result: {Result} for exit command", 
                                exitResponse.ErrorCode);
                        }
                        else
                        {
                            _logger.LogDebug("Auto-resume: VICE is running after {CommandType}", 
                                commandType);
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("Skipping auto-resume: Command {CommandType} returned {ErrorCode}", 
                        pending.Command.GetType().Name, response.ErrorCode);
                }

                _currentRequestId++;
                pending.Command.SetResult(response);
            }
            catch (Exception ex)
            {
                pending.Command.SetException(ex);
                throw;
            }
        }

        /// <summary>
        /// Checks if VICE is paused by reading the jiffy clock twice
        /// </summary>
        private async Task<bool> IsVicePausedAsync(CancellationToken ct)
        {
            try
            {
                // Read jiffy clock at $A0-$A2
                var readCmd1 = new MemoryGetCommand(0, 0x00A0, 0x00A2, MemSpace.MainMemory, 0);
                _currentRequestId++;
                await SendCommandAsync(_socket!, _currentRequestId, readCmd1, ct);
                var response1 = await WaitForResponseAsync(_currentRequestId, ct) as MemoryGetResponse;
                
                if (response1?.Memory == null) return false;
                
                var jiffy1 = new byte[3];
                using (var buffer1 = response1.Memory.Value)
                {
                    Array.Copy(buffer1.Data, jiffy1, 3);
                }
                
                // Small delay
                await Task.Delay(50, ct);
                
                // Read again
                var readCmd2 = new MemoryGetCommand(0, 0x00A0, 0x00A2, MemSpace.MainMemory, 0);
                _currentRequestId++;
                await SendCommandAsync(_socket!, _currentRequestId, readCmd2, ct);
                var response2 = await WaitForResponseAsync(_currentRequestId, ct) as MemoryGetResponse;
                
                if (response2?.Memory == null) return false;
                
                using (var buffer2 = response2.Memory.Value)
                {
                    // If jiffy clock hasn't changed, VICE is paused
                    bool isPaused = jiffy1[0] == buffer2.Data[0] && 
                                   jiffy1[1] == buffer2.Data[1] && 
                                   jiffy1[2] == buffer2.Data[2];
                    
                    _logger.LogDebug("Jiffy clock check: {J1:X2}{J2:X2}{J3:X2} vs {B1:X2}{B2:X2}{B3:X2} - Paused: {IsPaused}",
                        jiffy1[0], jiffy1[1], jiffy1[2],
                        buffer2.Data[0], buffer2.Data[1], buffer2.Data[2],
                        isPaused);
                    
                    return isPaused;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check if VICE is paused");
                return false;
            }
        }

        /// <summary>
        /// Waits asynchronously for a response corresponding to the specified request ID
        /// or until the operation is canceled or times out.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request for which the response is being awaited.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A <see cref="ViceResponse"/> object containing the response details, if received successfully.
        /// </returns>
        /// <exception cref="TimeoutException">
        /// Thrown if the waiting operation exceeds the allotted timeout period.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled using the provided cancellation token.
        /// </exception>
        private async Task<ViceResponse> WaitForResponseAsync(uint requestId, CancellationToken ct)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(5);

            // Special handling for CheckpointListCommand
            var checkpointInfos = new List<CheckpointInfoResponse>();

            while (!ct.IsCancellationRequested)
            {
                // Check if we already have the response in our incoming data
                var response = await TryReadResponseAsync(requestId, ct, checkpointInfos);
                if (response != null)
                {
                    // If this is a CheckpointListResponse, add collected infos
                    if (response is CheckpointListResponse listResponse && checkpointInfos.Count > 0)
                    {
                        return listResponse with { Info = checkpointInfos.ToImmutableArray() };
                    }
                    return response;
                }

                // Check timeout
                if (DateTime.UtcNow - startTime > timeout)
                    throw new TimeoutException($"Timeout waiting for response to request {requestId}");

                // Wait for more data
                await Task.Delay(10, ct);
            }

            throw new OperationCanceledException();
        }

        /// <summary>
        /// Asynchronously attempts to read a response from the VICE emulator and matches it to the specified target request ID.
        /// If the response does not match the target request ID, it is handled as an unmatched response for another request or as a broadcast message.
        /// </summary>
        /// <param name="targetRequestId">
        /// The unique identifier of the target request for which the response is being awaited.
        /// </param>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> used to cancel the operation if needed.
        /// </param>
        /// <param name="checkpointInfos">
        /// An optional list used to collect intermediate <see cref="CheckpointInfoResponse"/> objects associated with the target request.
        /// </param>
        /// <returns>
        /// A <see cref="ViceResponse"/> object representing the matched response to the specified target request ID,
        /// or null if no matching response is found or the operation is canceled.
        /// </returns>
        private async Task<ViceResponse?> TryReadResponseAsync(uint targetRequestId, CancellationToken ct, List<CheckpointInfoResponse>? checkpointInfos = null)
        {
            if (_socket == null || _socket.Available < 12)
                return null;

            var (response, requestId) = await ReadResponseAsync(_socket, ct);

            if (requestId == targetRequestId)
            {
                // Special handling for checkpoint info responses (they come before the list response)
                if (response is CheckpointInfoResponse info && checkpointInfos != null)
                {
                    checkpointInfos.Add(info);
                    MessagesHistory.AddsResponseOnly(response);
                    _logger.LogDebug("Collected CheckpointInfoResponse for request {RequestId}", requestId);
                    return null; // Keep collecting
                }

                _logger.LogDebug("Found response for request {RequestId}: {Type}", requestId, response.GetType().Name);
                return response;
            }

            // It's a different response (broadcast or for another request)
            _logger.LogDebug("Received unmatched response {Type} for request {RequestId}", response.GetType().Name, requestId);
            
            
            MessagesHistory.AddsResponseOnly(response);
            ViceResponse?.Invoke(this, new ViceResponseEventArgs(response));

            return null;
        }

        /// <summary>
        /// Processes incoming data asynchronously from the VICE emulator socket and handles responses.
        /// </summary>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> used to propagate notification that the operation should be canceled.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        private async Task ProcessIncomingDataAsync(CancellationToken ct)
        {
            while (_socket?.Available > 0)
            {
                try
                {
                    var (response, requestId) = await ReadResponseAsync(_socket, ct);
                    _logger.LogDebug("Processing incoming {Type} for request {RequestId}", response.GetType().Name, requestId);
                    
                    
                    MessagesHistory.AddsResponseOnly(response);
                    ViceResponse?.Invoke(this, new ViceResponseEventArgs(response));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing incoming data");
                    break;
                }
            }
        }

        /// <summary>
        /// Reads a response from the specified socket, parsing the header and body to create a ViceResponse with its associated request ID.
        /// </summary>
        /// <param name="socket">
        /// The socket from which the response data will be read.
        /// </param>
        /// <param name="ct">
        /// The cancellation token used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A tuple containing the parsed <see cref="ViceResponse"/> and the associated request ID.
        /// </returns>
        private async Task<(ViceResponse Response, uint RequestId)> ReadResponseAsync(Socket socket, CancellationToken ct)
        {
            using var headerBuffer = _byteArrayPool.GetBuffer(12);
            await ReadExactBytesAsync(socket, headerBuffer.Data.AsMemory(0, 12), ct);

            uint bodyLength = _responseBuilder.GetResponseBodyLength(headerBuffer.Data.AsSpan());

            if (bodyLength > 0)
            {
                using var bodyBuffer = _byteArrayPool.GetBuffer(bodyLength);
                await ReadExactBytesAsync(socket, bodyBuffer.Data.AsMemory(0, (int)bodyLength), ct);
                return _responseBuilder.Build(headerBuffer.Data.AsSpan(), ViceCommand.DefaultApiVersion,
                    bodyBuffer.Data.AsSpan(0, (int)bodyLength));
            }

            return _responseBuilder.Build(headerBuffer.Data.AsSpan(), ViceCommand.DefaultApiVersion, Array.Empty<byte>());
        }

        /// <summary>
        /// Sends a command asynchronously to the VICE emulator using the specified socket and request ID.
        /// </summary>
        /// <param name="socket">
        /// The socket through which the command will be sent.
        /// </param>
        /// <param name="requestId">
        /// The unique identifier for the request associated with this command.
        /// </param>
        /// <param name="command">
        /// The command to be sent, implementing the <see cref="IViceCommand"/> interface.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation of sending the command.
        /// </returns>
        private async Task SendCommandAsync(Socket socket, uint requestId, IViceCommand command, CancellationToken ct)
        {
            _logger.LogDebug("Sending {CommandType} with request ID {RequestId}", command.GetType().Name, requestId);

            var (buffer, length) = command.GetBinaryData(requestId);
            try
            {
                await SendExactBytesAsync(socket, buffer.Data.AsMemory(0, (int)length), ct);

                PerformanceProfiler.Add(new CommandSentEvent(command.GetType(), PerformanceProfiler.Ticks));
                await MessagesHistory.AddCommandAsync(requestId, command);
            }
            finally
            {
                buffer.Dispose();
            }
        }

        /// <summary>
        /// Reads a specified amount of bytes from the given socket into the provided buffer.
        /// Continues reading until the buffer is completely filled or a disconnection occurs.
        /// </summary>
        /// <param name="socket">
        /// The socket from which to read data.
        /// </param>
        /// <param name="buffer">
        /// The memory buffer where the received data will be stored.
        /// </param>
        /// <param name="ct">
        /// A cancellation token used to observe cancellation requests and abort the operation if needed.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. Completes when the specified number
        /// of bytes has been read into the buffer, or throws an exception if the socket disconnects prematurely.
        /// </returns>
        private async Task ReadExactBytesAsync(Socket socket, Memory<byte> buffer, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

                int read = await socket.ReceiveAsync(buffer[totalRead..], SocketFlags.None, timeoutCts.Token);
                if (read == 0)
                    throw new SocketDisconnectedException("Socket disconnected while reading");

                totalRead += read;
            }
        }

        /// <summary>
        /// Sends the exact number of bytes specified from the given data buffer to the provided socket.
        /// Ensures that all bytes are transmitted, or throws an exception if the socket disconnects during the operation.
        /// </summary>
        /// <param name="socket">
        /// The socket through which the data will be sent.
        /// </param>
        /// <param name="data">
        /// A read-only memory buffer containing the data to be sent.
        /// </param>
        /// <param name="ct">
        /// A cancellation token used to observe cancellation requests.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation of sending data. Completes once all bytes are successfully sent or an error occurs.
        /// </returns>
        /// <exception cref="SocketDisconnectedException">
        /// Thrown if the socket disconnects before all bytes are transmitted.
        /// </exception>
        private async Task SendExactBytesAsync(Socket socket, ReadOnlyMemory<byte> data, CancellationToken ct)
        {
            int totalSent = 0;
            while (totalSent < data.Length)
            {
                int sent = await socket.SendAsync(data[totalSent..], SocketFlags.None, ct);
                if (sent == 0)
                    throw new SocketDisconnectedException("Socket disconnected while sending");

                totalSent += sent;
            }
        }

        /// <summary>
        /// Waits for a specific port to become available by polling active TCP listeners.
        /// </summary>
        /// <param name="port">
        /// The port number to monitor for availability.
        /// </param>
        /// <param name="ct">
        /// The cancellation token used to observe cancellation requests.
        /// </param>
        /// <returns>
        /// A task that completes when the specified port is available or when the cancellation token is triggered.
        /// </returns>
        private static async Task WaitForPort(int port, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var listeners = properties.GetActiveTcpListeners();

                if (listeners.Any(l => l.Port == port))
                    return;

                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
            }
        }

        /// <summary>
        /// Closes the currently active socket connection, terminating communication with the VICE emulator.
        /// Ensures the socket is properly shut down, closed, and disposed of to release system resources.
        /// </summary>
        private void CloseSocket()
        {
            if (_socket?.Connected == true)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch { }
            }
            _socket?.Dispose();
            _socket = null;
            IsConnected = false;
        }

        /// <summary>
        /// Waits for a connection status change event to occur asynchronously.
        /// </summary>
        /// <param name="ct">
        /// A cancellation token to observe while waiting for the connection status change.
        /// If cancellation is requested, the task will be canceled.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous wait operation.
        /// The result is <c>true</c> if the connection is established or <c>false</c> if the connection is lost.
        /// </returns>
        public async Task<bool> WaitForConnectionStatusChangeAsync(CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<ConnectedChangedEventArgs>? handler = null;
            handler = (_, e) =>
            {
                tcs.TrySetResult(e.IsConnected);
                ConnectedChanged -= handler;
            };
            ConnectedChanged += handler;

            ct.Register(() =>
            {
                tcs.TrySetCanceled();
                ConnectedChanged -= handler;
            });

            return await tcs.Task;
        }

        /// <summary>
        /// Disposes resources used by the ViceBridge instance asynchronously.
        /// </summary>
        /// <returns>
        /// A ValueTask representing the completion of the asynchronous dispose operation.
        /// </returns>
        public async ValueTask DisposeAsync()
        {
            await StopAsync(false);
        }

        /// <summary>
        /// Releases all resources used by the ViceBridge instance.
        /// Ensures that the connection to the VICE emulator is stopped
        /// and properly disposed of to release any held resources.
        /// </summary>
        public void Dispose()
        {
            StopAsync(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Represents a command queued for execution in the VICE emulator communication process.
        /// Encapsulates the command to be executed and a flag indicating whether the command should
        /// resume processing even if the bridge is in a stopped state.
        /// </summary>
        private record PendingCommand(IViceCommand Command, bool ResumeOnStopped);
    }
}
