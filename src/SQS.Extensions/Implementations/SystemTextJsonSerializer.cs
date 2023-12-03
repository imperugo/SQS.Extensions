using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Serialization.Json;

namespace SQS.Extensions.Implementations;

/// <summary>
/// System.Text.Json implementation of <see cref="IMessageSerializer"/>
/// </summary>
public sealed partial class SystemTextJsonSerializer : IMessageSerializer
{
    private readonly ILogger<SystemTextJsonSerializer> logger;
    private readonly JsonSerializerOptions defaultSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonSerializer"/> class.
    /// </summary>
    public SystemTextJsonSerializer(ILogger<SystemTextJsonSerializer> logger, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.defaultSerializer = serviceProvider.GetService<JsonSerializerOptions>() ?? SerializationOptions.Default;
    }

    /// <inheritdoc/>
    public string Serialize<T>(T itemToSerialized)
    {
        return JsonSerializer.Serialize(itemToSerialized, defaultSerializer);
    }

    /// <inheritdoc/>
    public T? Deserialize<T>(string serializedObject)
    {
        var result = JsonSerializer.Deserialize<T>(serializedObject, defaultSerializer);

        if (serializedObject.Length > 0 && result == null)
            LogDeserializingProblem(logger, serializedObject, typeof(T));

        return result;
    }

    [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "There was an error deserializing {serializedObject} to {type}")]
    private static partial void LogDeserializingProblem(
        ILogger logger,
        string serializedObject,
        Type? type);
}
