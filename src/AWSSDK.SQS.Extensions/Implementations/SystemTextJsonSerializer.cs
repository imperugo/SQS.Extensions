using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.DependencyInjection;

using AWSSDK.SQS.Extensions.Abstractions;
using AWSSDK.SQS.Extensions.Serialization.Json;

using Microsoft.Extensions.Logging;

namespace AWSSDK.SQS.Extensions.Implementations;

/// <summary>
/// System.Text.Json implementation of <see cref="IMessageSerializer"/>
/// </summary>
public sealed partial class SystemTextJsonSerializer : IMessageSerializer
{
    private readonly IServiceProvider? applicationServices;
    private readonly ILogger<SystemTextJsonSerializer> logger;
    private readonly JsonSerializerOptions defaultSerializer = SerializationOptions.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonSerializer"/> class.
    /// </summary>
    public SystemTextJsonSerializer(ILogger<SystemTextJsonSerializer> logger)
        : this(null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonSerializer"/> class.
    /// </summary>
    /// <param name="applicationServices">An instance of <see cref="IServiceProvider"/>.</param>
    /// <param name="logger">An instance of <see cref="ILogger{SystemTextJsonSerializer}"/>.</param>
    public SystemTextJsonSerializer(IServiceProvider? applicationServices, ILogger<SystemTextJsonSerializer> logger)
    {
        this.applicationServices = applicationServices;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public string Serialize<T>(T itemToSerialized)
    {
        var serializationContext = applicationServices?.GetService<JsonTypeInfo<T>>();

        return serializationContext != null
            ? JsonSerializer.Serialize(itemToSerialized, serializationContext)
            : JsonSerializer.Serialize(itemToSerialized, defaultSerializer);
    }

    /// <inheritdoc/>
    public T? Deserialize<T>(string serializedObject)
    {
        var serializationContext = applicationServices?.GetService<JsonTypeInfo<T>>();

        var result = serializationContext != null
            ? JsonSerializer.Deserialize(serializedObject, serializationContext)
            : JsonSerializer.Deserialize<T>(serializedObject, defaultSerializer);

        if (serializedObject?.Length > 0 && result == null)
            LogDeserializingProblen(logger, serializedObject, typeof(T));

        return result;
    }

    [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "There was an error deserializing {serializedObject} to {type}")]
    private static partial void LogDeserializingProblen(
        ILogger logger,
        string serializedObject,
        Type? type);
}
