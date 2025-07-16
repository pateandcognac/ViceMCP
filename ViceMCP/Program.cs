using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ViceMCP;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.AddConsole(console =>
        {
            console.LogToStandardErrorThreshold = LogLevel.Information;
        });

        // Add VICE configuration from environment variables
        builder.Services.AddSingleton(ViceConfiguration.FromEnvironment());
        
        // Add ViceBridge services
        builder.Services.AddSingleton<Righthand.ViceMonitor.Bridge.Responses.ResponseBuilder>();
        builder.Services.AddSingleton<Righthand.ViceMonitor.Bridge.Services.Abstract.IPerformanceProfiler, EmptyPerformanceProfiler>();
        builder.Services.AddSingleton<Righthand.ViceMonitor.Bridge.Services.Abstract.IMessagesHistory, SimpleMessagesHistory>();
        builder.Services.AddSingleton<Righthand.ViceMonitor.Bridge.Services.Abstract.IViceBridge, Righthand.ViceMonitor.Bridge.Services.Implementation.ViceBridge>();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        await builder.Build().RunAsync();
    }
}