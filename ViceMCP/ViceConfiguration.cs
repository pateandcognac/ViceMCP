namespace ViceMCP;

/// <summary>
/// Configuration for VICE emulator paths and settings
/// </summary>
public class ViceConfiguration
{
    /// <summary>
    /// Base path where VICE binaries are located
    /// </summary>
    public string ViceBinPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Port for the binary monitor (default: 6502)
    /// </summary>
    public int BinaryMonitorPort { get; set; } = 6502;
    
    /// <summary>
    /// Timeout in milliseconds to wait for VICE to start (default: 2000)
    /// </summary>
    public int StartupTimeout { get; set; } = 2000;
    
    /// <summary>
    /// Creates configuration from environment variables
    /// </summary>
    public static ViceConfiguration FromEnvironment()
    {
        var config = new ViceConfiguration();
        
        // Get VICE binary path from environment
        var vicePath = Environment.GetEnvironmentVariable("VICE_BIN_PATH");
        if (!string.IsNullOrEmpty(vicePath))
        {
            config.ViceBinPath = vicePath;
        }
        
        // Get binary monitor port from environment
        var portStr = Environment.GetEnvironmentVariable("VICE_MONITOR_PORT");
        if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out var port))
        {
            config.BinaryMonitorPort = port;
        }
        
        // Get startup timeout from environment
        var timeoutStr = Environment.GetEnvironmentVariable("VICE_STARTUP_TIMEOUT");
        if (!string.IsNullOrEmpty(timeoutStr) && int.TryParse(timeoutStr, out var timeout))
        {
            config.StartupTimeout = timeout;
        }
        
        return config;
    }
    
    /// <summary>
    /// Gets the full path to a VICE emulator binary
    /// </summary>
    public string GetEmulatorPath(string emulatorName)
    {
        if (string.IsNullOrEmpty(ViceBinPath))
        {
            // If no base path is set, try common locations
            return FindEmulatorInCommonPaths(emulatorName);
        }
        
        // Combine base path with emulator name
        var fullPath = Path.Combine(ViceBinPath, emulatorName);
        
        // On Windows, add .exe if not present
        if (OperatingSystem.IsWindows() && !fullPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            fullPath += ".exe";
        }
        
        return fullPath;
    }
    
    private static string FindEmulatorInCommonPaths(string emulatorName)
    {
        var paths = new List<string>();
        
        if (OperatingSystem.IsWindows())
        {
            var exeName = emulatorName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) 
                ? emulatorName 
                : emulatorName + ".exe";
                
            paths.AddRange(new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VICE", exeName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "VICE", exeName),
                Path.Combine(@"C:\VICE", exeName),
                exeName // Try PATH
            });
        }
        else if (OperatingSystem.IsMacOS())
        {
            paths.AddRange(new[]
            {
                $"/usr/local/bin/{emulatorName}",
                $"/opt/homebrew/bin/{emulatorName}",
                $"/Applications/VICE.app/Contents/MacOS/{emulatorName}",
                emulatorName // Try PATH
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            paths.AddRange(new[]
            {
                $"/usr/bin/{emulatorName}",
                $"/usr/local/bin/{emulatorName}",
                $"/opt/vice/bin/{emulatorName}",
                emulatorName // Try PATH
            });
        }
        
        // Return the first path that exists, or just the emulator name to let the system try PATH
        return paths.FirstOrDefault(File.Exists) ?? emulatorName;
    }
}