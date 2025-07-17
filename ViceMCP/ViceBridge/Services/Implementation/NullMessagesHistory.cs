using ViceMCP.ViceBridge.Commands;
using ViceMCP.ViceBridge.Responses;
using ViceMCP.ViceBridge.Services.Abstract;

namespace ViceMCP.ViceBridge.Services.Implementation;

/// <summary>
/// Provides a no-op service for message history.
/// </summary>
public class NullMessagesHistory : IMessagesHistory
{
    ///<inheritdoc/>
    ValueTask<int> IMessagesHistory.AddCommandAsync(uint sequence, IViceCommand? command) => new ValueTask<int>(0);
    ///<inheritdoc/>
    void IMessagesHistory.AddsResponseOnly(ViceResponse response)
    { }
    ///<inheritdoc/>
    void IMessagesHistory.Start()
    { }
    ///<inheritdoc/>
    void IMessagesHistory.UpdateWithLinkedResponse(int id, ViceResponse response)
    { }
    ///<inheritdoc/>
    void IMessagesHistory.UpdateWithResponse(int id, ViceResponse response)
    { }
}
