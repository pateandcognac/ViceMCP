using Microsoft.Extensions.Logging;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using Righthand.ViceMonitor.Bridge.Services.Implementation;
using Righthand.ViceMonitor.Bridge.Shared;

Console.WriteLine("Testing vice-bridge-net library directly...");

// Create ViceBridge with required dependencies
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ViceBridge>();
var responseBuilderLogger = loggerFactory.CreateLogger<ResponseBuilder>();
var responseBuilder = new ResponseBuilder(responseBuilderLogger);
var performanceProfiler = new NoOpPerformanceProfiler();
var messagesHistory = new NoOpMessagesHistory();
var viceBridge = new ViceBridge(logger, responseBuilder, performanceProfiler, messagesHistory);

try
{
    Console.WriteLine("Starting VICE connection on port 6502...");
    viceBridge.Start(6502);
    
    await Task.Delay(1000); // Give it time to connect
    
    // Test 1: Simple memory write WITHOUT resumeOnStopped
    Console.WriteLine("Test 1: Memory write without auto-resume...");
    using var buffer1 = BufferManager.GetBuffer(1);
    buffer1.Data[0] = 0x0E; // Light blue border
    
    var memWriteCmd1 = new MemorySetCommand(0, 0xD020, MemSpace.MainMemory, 0, buffer1);
    var result1 = await viceBridge.EnqueueCommand(memWriteCmd1).Response;
    Console.WriteLine($"Result 1: {result1.IsSuccess} - {result1.ErrorCode}");
    
    await Task.Delay(500);
    
    // Test 2: Memory write WITH resumeOnStopped = true
    Console.WriteLine("Test 2: Memory write with auto-resume...");
    using var buffer2 = BufferManager.GetBuffer(1);
    buffer2.Data[0] = 0x06; // Blue background
    
    var memWriteCmd2 = new MemorySetCommand(0, 0xD021, MemSpace.MainMemory, 0, buffer2);
    var result2 = await viceBridge.EnqueueCommand(memWriteCmd2, resumeOnStopped: true).Response;
    Console.WriteLine($"Result 2: {result2.IsSuccess} - {result2.ErrorCode}");
    
    await Task.Delay(500);
    
    // Test 3: Register write WITH resumeOnStopped = true
    Console.WriteLine("Test 3: Register write with auto-resume...");
    var items = System.Collections.Immutable.ImmutableArray.Create(new RegisterItem(0, 0x42)); // A = $42
    var regWriteCmd = new RegistersSetCommand(MemSpace.MainMemory, items);
    var result3 = await viceBridge.EnqueueCommand(regWriteCmd, resumeOnStopped: true).Response;
    Console.WriteLine($"Result 3: {result3.IsSuccess} - {result3.ErrorCode}");
    
    Console.WriteLine("All tests completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
finally
{
    Console.WriteLine("Cleaning up...");
    viceBridge.Dispose();
}

// Simple implementations of required interfaces
public class NoOpPerformanceProfiler : IPerformanceProfiler
{
    public bool IsEnabled => false;
    public System.Collections.Immutable.ImmutableArray<PerformanceEvent> Events => System.Collections.Immutable.ImmutableArray<PerformanceEvent>.Empty;
    public long Ticks => 0;
    public void Add(PerformanceEvent performanceEvent) { }
    public void Clear() { }
}

public class NoOpMessagesHistory : IMessagesHistory
{
    public void Start() { }
    public ValueTask<int> AddCommandAsync(uint sequence, IViceCommand? command) => ValueTask.FromResult(0);
    public void UpdateWithResponse(int id, ViceResponse response) { }
    public void UpdateWithLinkedResponse(int id, ViceResponse response) { }
    public void AddsResponseOnly(ViceResponse response) { }
}