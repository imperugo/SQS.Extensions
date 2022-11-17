namespace AWSSDK.SQS.Extensions.Configurations;

/// <summary>
/// The message pump configuration class.
/// </summary>
public sealed record MessagePumpConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePumpConfiguration"/> class.
    /// </summary>
    /// <param name="queueName">The name of the SQS queue</param>
    public MessagePumpConfiguration(string queueName)
    {
        QueueName = queueName;
    }

    /// <summary>
    /// If is set to <c>True</c>, the specified <see cref="QueueName"/> will  be purged before the first message is received.
    /// </summary>
    public bool PurgeOnStartup { get; set; }

    /// <summary>
    ///  The name of the queue to use
    /// </summary>
    public string QueueName { get; }

    /// <summary>
    /// The maximum number of concurrent operation. Out of the box the number of concurrent operations starts form a minumum of 2 and increases to the number of processor.
    /// </summary>
    public int MaxConcurrentOperation { get; set; } = Math.Max(2, Environment.ProcessorCount);

    /// <summary>
    /// How long the system must wait before it will try to receive a set of messages again.
    /// </summary>
    public TimeSpan BatchDelay { get; set; } = TimeSpan.Zero;
}
