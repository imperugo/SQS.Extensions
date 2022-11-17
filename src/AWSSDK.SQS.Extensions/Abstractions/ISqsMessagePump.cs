namespace AWSSDK.SQS.Extensions.Abstractions;

/// <summary>
/// Contract for the message pump
/// </summary>
public interface ISqsMessagePump<T>
{
    /// <summary>
    /// Start pump messages from SQS.
    /// </summary>
    /// <param name="processMessageAsync">The function to call when a message has been caught.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PumpAsync(Func<T?, CancellationToken, Task> processMessageAsync, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop pumping message from SQS to the client.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task StopPumpAsync(CancellationToken cancellationToken = default);
}
