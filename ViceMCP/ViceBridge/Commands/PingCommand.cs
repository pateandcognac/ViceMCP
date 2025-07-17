using ViceMCP.ViceBridge.Responses;

namespace ViceMCP.ViceBridge.Commands
{
    /// <summary>
    /// Get an empty response.
    /// </summary>
    public record PingCommand() : ParameterlessCommand<EmptyViceResponse>(CommandType.Ping)
    { }
}
