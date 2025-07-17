using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViceMCP;

public class BatchCommandSpec
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";
    
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class BatchResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("result")]
    public string Result { get; set; } = "";
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class BatchResponse
{
    [JsonPropertyName("total_commands")]
    public int TotalCommands { get; set; }
    
    [JsonPropertyName("successful_commands")]
    public int SuccessfulCommands { get; set; }
    
    [JsonPropertyName("failed_commands")]
    public int FailedCommands { get; set; }
    
    [JsonPropertyName("results")]
    public List<BatchResult> Results { get; set; } = new();
    
    [JsonPropertyName("execution_time_ms")]
    public long ExecutionTimeMs { get; set; }
}