using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;

namespace SQS.Extensions;

/// <summary>
/// The base implementation of SQS Consumer
/// </summary>
public abstract partial class SqsHostedService<T> : BackgroundService
{
    private readonly ISqsMessagePumpFactory messagePumpFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsHostedService{T}"/> class.
    /// </summary>
    protected SqsHostedService(ILogger logger, ISqsMessagePumpFactory messagePumpFactory)
    {
        this.Logger = logger;
        this.messagePumpFactory = messagePumpFactory;
    }

    /// <summary>
    /// The function to call when a message has been caught from the queue.
    /// </summary>
    protected abstract Func<T?, CancellationToken, Task> ProcessMessageFunc { get; }

    /// <summary>
    /// The pump configuration
    /// </summary>
    protected abstract MessagePumpConfiguration MessagePumpConfiguration { get; }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            LogHostServiceStarted(Logger);
            var pump = await messagePumpFactory.CreateAsync<T>(MessagePumpConfiguration, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await pump.PumpAsync(ProcessMessageFunc, cancellationToken);
                }
                catch (Exception e)
                {
                    LogPumpError(Logger, e);
                }

                await Task.Delay(MessagePumpConfiguration.BatchDelay, cancellationToken);
            }
        }
        catch (Exception e)
        {
            LogCriticalError(Logger, e.Message, e);
        }
    }

    /// <summary>
    /// Gets the logger used by the current class.
    /// </summary>
    /// <value>
    /// The logger used for logging in the current class.
    /// </value>
    protected ILogger Logger { get ; }

    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Sqs Hosted Service is starting")]
    private static partial void LogHostServiceStarted(ILogger logger);

    [LoggerMessage(EventId = 102, Level = LogLevel.Error, Message = "[MessagePump] There is an error while processing messages")]
    private static partial void LogPumpError(ILogger logger, Exception exc);

    [LoggerMessage(EventId = 103, Level = LogLevel.Critical, Message = "The Sqs Hosted Service is shutting down. Reason: {Message}")]
    private static partial void LogCriticalError(ILogger logger, string message, Exception exc);
}
