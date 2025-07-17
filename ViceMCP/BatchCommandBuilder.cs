using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ViceMCP;

public class BatchCommandBuilder
{
    private readonly ViceTools _viceTools;
    private readonly Dictionary<string, MethodInfo> _commandMethods;

    public BatchCommandBuilder(ViceTools viceTools)
    {
        _viceTools = viceTools;
        _commandMethods = BuildCommandMethodsMap();
    }

    private Dictionary<string, MethodInfo> BuildCommandMethodsMap()
    {
        var methods = new Dictionary<string, MethodInfo>();
        var type = typeof(ViceTools);
        
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var mcpAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
            if (mcpAttribute != null && !string.IsNullOrEmpty(mcpAttribute.Name))
            {
                methods[mcpAttribute.Name] = method;
            }
        }
        
        return methods;
    }

    public async Task<BatchResponse> ExecuteBatchAsync(List<BatchCommandSpec> commands, bool failFast = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new BatchResponse
        {
            TotalCommands = commands.Count,
            Results = new List<BatchResult>()
        };

        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            var result = new BatchResult
            {
                Command = command.Command,
                Description = command.Description
            };

            try
            {
                if (!_commandMethods.TryGetValue(command.Command, out var method))
                {
                    throw new InvalidOperationException($"Unknown command: {command.Command}");
                }

                var parameters = PrepareParameters(method, command.Parameters);
                var task = (Task)method.Invoke(_viceTools, parameters)!;
                await task;

                // Get the result from the task
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result.Result = resultProperty.GetValue(task)?.ToString() ?? "";
                }

                result.Success = true;
                response.SuccessfulCommands++;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.InnerException?.Message ?? ex.Message;
                response.FailedCommands++;

                if (failFast)
                {
                    response.Results.Add(result);
                    break;
                }
            }

            response.Results.Add(result);
        }

        stopwatch.Stop();
        response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        return response;
    }

    private object[] PrepareParameters(MethodInfo method, Dictionary<string, object> parameters)
    {
        var parameterInfos = method.GetParameters();
        var preparedParams = new object[parameterInfos.Length];

        for (int i = 0; i < parameterInfos.Length; i++)
        {
            var paramInfo = parameterInfos[i];
            var paramName = paramInfo.Name!;
            var paramType = paramInfo.ParameterType;

            if (parameters.TryGetValue(paramName, out var value))
            {
                preparedParams[i] = ConvertParameter(value, paramType);
            }
            else if (paramInfo.HasDefaultValue)
            {
                preparedParams[i] = paramInfo.DefaultValue!;
            }
            else
            {
                throw new ArgumentException($"Required parameter '{paramName}' not provided for command");
            }
        }

        return preparedParams;
    }

    private object ConvertParameter(object value, Type targetType)
    {
        // Handle nullable types
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (value == null)
                return null!;
            targetType = Nullable.GetUnderlyingType(targetType)!;
        }

        // Handle JsonElement from JSON deserialization
        if (value is JsonElement jsonElement)
        {
            return ConvertJsonElement(jsonElement, targetType);
        }

        // Direct conversion for compatible types
        if (targetType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        // Type conversion
        return Convert.ChangeType(value, targetType);
    }

    private object ConvertJsonElement(JsonElement element, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return element.GetString() ?? "";
        }
        if (targetType == typeof(int))
        {
            return element.GetInt32();
        }
        if (targetType == typeof(bool))
        {
            return element.GetBoolean();
        }
        if (targetType == typeof(uint))
        {
            return element.GetUInt32();
        }
        if (targetType == typeof(long))
        {
            return element.GetInt64();
        }
        if (targetType == typeof(double))
        {
            return element.GetDouble();
        }

        // For complex types, deserialize from JSON
        return JsonSerializer.Deserialize(element.GetRawText(), targetType)!;
    }
}