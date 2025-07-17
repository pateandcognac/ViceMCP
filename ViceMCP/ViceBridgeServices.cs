using System.Collections.Immutable;
using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Services.Abstract;
using ViceMCP.ViceBridge.Shared;

namespace ViceMCP;

// Simple implementations for ViceBridge dependencies
public class EmptyPerformanceProfiler : IPerformanceProfiler
{
    public bool IsEnabled => false;
    public ImmutableArray<PerformanceEvent> Events => ImmutableArray<PerformanceEvent>.Empty;
    public long Ticks => 0;
    public void Add(PerformanceEvent performanceEvent) { }
    public void Clear() { }
}

public class SimpleMessagesHistory : IMessagesHistory
{
    public void Start() { }
    public ValueTask<int> AddCommandAsync(uint sequence, IViceCommand? command) => ValueTask.FromResult(0);
    public void UpdateWithResponse(int id, ViceResponse response) { }
    public void UpdateWithLinkedResponse(int id, ViceResponse response) { }
    public void AddsResponseOnly(ViceResponse response) { }
}