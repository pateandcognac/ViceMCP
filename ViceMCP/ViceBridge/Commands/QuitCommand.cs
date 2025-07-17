using ViceMCP.ViceBridge.Responses;

namespace ViceMCP.ViceBridge.Commands
{
    /// <summary>
    /// Quits VICE. 
    /// </summary>
    public record QuitCommand() : ParameterlessCommand<EmptyViceResponse>(CommandType.Quit)
    { }
}
