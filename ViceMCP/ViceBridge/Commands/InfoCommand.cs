using ViceMCP.ViceBridge.Responses;

namespace ViceMCP.ViceBridge.Commands
{
    /// <summary>
    /// Retrieves VICE version.
    /// </summary>
    /// <remarks>This command might not yet be implemented in VICE stable version 3.5</remarks>
    public record InfoCommand() : ParameterlessCommand<InfoResponse>(CommandType.Info);
}
