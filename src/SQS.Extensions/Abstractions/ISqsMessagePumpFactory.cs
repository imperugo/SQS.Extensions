using SQS.Extensions.Configurations;

namespace SQS.Extensions.Abstractions;

/// <summary>
/// Contract for the message pump factory
/// </summary>
public interface ISqsMessagePumpFactory
{
    /// <summary>
    /// Create a new instance of message pump with automatic deserialization
    /// </summary>
    /// <param name="configuration">The pump configuration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <typeparam name="TMessage">The type to use during the message deserialization</typeparam>
    Task<ISqsMessagePump<TMessage>> CreateMessagePumpAsync<TMessage>(MessagePumpConfiguration configuration, CancellationToken cancellationToken) where TMessage : notnull;

    /// <summary>
    /// Create a new instance of bulk message pump with automatic deserialization
    /// </summary>
    /// <param name="configuration">The pump configuration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <typeparam name="TMessage">The type to use during the message deserialization</typeparam>
    Task<ISqsBulkMessagePump<TMessage>> CreateBulkMessagePumpAsync<TMessage>(MessagePumpConfiguration configuration, CancellationToken cancellationToken) where TMessage : notnull;
}
