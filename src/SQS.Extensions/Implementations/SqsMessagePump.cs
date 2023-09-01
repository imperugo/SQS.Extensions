using System.Diagnostics;

using Amazon.SQS;
using Amazon.SQS.Model;

using Microsoft.Extensions.Logging;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;
using SQS.Extensions.OpenTelemetry.Metrics;

namespace SQS.Extensions.Implementations;

/// <summary>
/// The implementation of <see cref="ISqsMessagePump{T}"/>
/// </summary>
internal sealed partial class SqsMessagePump<T> : ISqsMessagePump<T>, IAsyncDisposable
{
    private readonly IAmazonSQS sqsService;
    private readonly ILogger logger;
    private readonly MessagePumpConfiguration configuration;
    private readonly ISqsQueueHelper queueHelper;
    private readonly IMessageSerializer messageSerializer;

#if NET6_0_OR_GREATER
#else
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
#endif

    private readonly CancellationTokenSource messagePumpCancellationTokenSource = new();
    private readonly CancellationTokenSource messageProcessingCancellationTokenSource = new();
    private TagList tagList;
    private readonly Task[] pumpTasks;
    private readonly int numberOfPumps;
    private readonly int numberOfMessagesToFetch;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsMessagePump{T}"/> class.
    /// </summary>
    public SqsMessagePump(
        IAmazonSQS sqsService,
        ILogger logger,
        MessagePumpConfiguration configuration,
        ISqsQueueHelper queueHelper,
        IMessageSerializer messageSerializer)
    {
        this.sqsService = sqsService;
        this.logger = logger;
        this.configuration = configuration;
        this.queueHelper = queueHelper;
        this.messageSerializer = messageSerializer;

        // 10 is a limit of SQS reason why I've to scale with the pump
        if (configuration.MaxConcurrentOperation <= 10)
        {
            numberOfPumps = 1;
            numberOfMessagesToFetch = configuration.MaxConcurrentOperation;
        }
        else
        {
            numberOfMessagesToFetch = 10;
            numberOfPumps = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(configuration.MaxConcurrentOperation) / numberOfMessagesToFetch));
        }

        pumpTasks = new Task[numberOfPumps];
        tagList = new TagList(new KeyValuePair<string, object>[]
            {
                new(MeterTags.QueueName, configuration.QueueName),
                new(MeterTags.MessageType, typeof(T))
            }
            .AsSpan()!);
    }

    internal async Task InitAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.PurgeOnStartup)
            return;

        try
        {
            var queueUrl = await queueHelper.GetQueueUrlAsync(configuration.QueueName);
            await sqsService.PurgeQueueAsync(queueUrl, cancellationToken).ConfigureAwait(false);

            LogQueuePurge(logger, queueUrl);
        }
        catch (PurgeQueueInProgressException ex)
        {
            LogPurgeError(logger, ex);
        }
    }

    /// <inheritdoc/>
    public async Task PumpAsync(Func<T?, MessageContext, CancellationToken, Task> processMessageAsync, CancellationToken cancellationToken = default)
    {
        var queueUrl = await queueHelper.GetQueueUrlAsync(configuration.QueueName);

        var receiveMessagesRequest = new ReceiveMessageRequest
        {
            MaxNumberOfMessages = numberOfMessagesToFetch,
            QueueUrl = queueUrl,
            WaitTimeSeconds = 20,
            AttributeNames = new List<string>
            {
                "SentTimestamp",
                "MessageTTL"
            }
        };

        for (var i = 0; i < numberOfPumps; i++)
            pumpTasks[i] = ConsumeMessagesAsync(processMessageAsync, receiveMessagesRequest, cancellationToken);

        await Task.WhenAll(pumpTasks);
    }

    /// <inheritdoc/>
    public async Task StopPumpAsync(CancellationToken cancellationToken = default)
    {
        messagePumpCancellationTokenSource.Cancel();

#if NET6_0_OR_GREATER
        await using (cancellationToken.Register(() => messageProcessingCancellationTokenSource.Cancel()))
#else
        using (cancellationToken.Register(() => messageProcessingCancellationTokenSource.Cancel()))
#endif
            await Task.WhenAll(pumpTasks).ConfigureAwait(false);

        messagePumpCancellationTokenSource.Dispose();
        messageProcessingCancellationTokenSource.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopPumpAsync().ConfigureAwait(false);
    }

    private async Task ConsumeMessagesAsync(Func<T?, MessageContext, CancellationToken, Task> processMessageAsync, ReceiveMessageRequest messageRequest, CancellationToken cancellationToken)
    {
        var receivedMessages = await sqsService.ReceiveMessageAsync(messageRequest, cancellationToken).ConfigureAwait(false);

        var queueUrl = await queueHelper.GetQueueUrlAsync(configuration.QueueName);

        LogMessageReceived(logger, receivedMessages.Messages.Count, queueUrl);

        Meters.TotalFetched.Add(receivedMessages.Messages.Count, tagList);

        Func<Message, MessageContext> getContext = (message) =>
        {
            var messageContext = new MessageContext()
            {
                MessageId = message.MessageId,
                MessageAttributes = message.Attributes
            };

            if (message.Attributes.TryGetValue("ApproximateReceiveCount", out var receiveCount))
                messageContext.RetryCount = int.Parse(receiveCount);

            return messageContext;
        };

#if NET6_0_OR_GREATER
        await Parallel.ForEachAsync(receivedMessages.Messages, cancellationToken, async (message, token) =>
        {
            var ctx = getContext(message);
            await ProcessMessageAsync(processMessageAsync, message, ctx, token).ConfigureAwait(false);
        });
#else
        var tasks = new Task[receivedMessages.Messages.Count];

        for (var i = 0; i < receivedMessages.Messages.Count; i++)
        {
            var message = receivedMessages.Messages[i];
            tasks[i] = ProcessMessageAsync(processMessageAsync, message, ctx, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif

    }

    private async Task ProcessMessageAsync(Func<T?, MessageContext, CancellationToken, Task> processMessageAsync, Message message, MessageContext context, CancellationToken cancellationToken)
    {
        if (!IsMessageExpired(message, tagList))
        {
            try
            {
                await processMessageAsync(messageSerializer.Deserialize<T>(message.Body), context, messageProcessingCancellationTokenSource.Token).ConfigureAwait(false);
                Meters.TotalProcessedSuccessfully.Add(1, tagList);
            }
            catch (Exception ex)
            {
                tagList.Add(new KeyValuePair<string, object?>(MeterTags.FailureType, ex.GetType()));
                Meters.TotalFailures.Add(1, tagList);
                throw;
            }
        }

        // Always delete the message from the queue.
        // If processing failed, the onError handler will have moved the message
        // to a retry queue.
        await DeleteMessageAndBodyIfRequiredAsync(message, cancellationToken).ConfigureAwait(false);
        Meters.TotalDeleted.Add(1, tagList);
    }

    private async Task DeleteMessageAndBodyIfRequiredAsync(Message message, CancellationToken token)
    {
        try
        {
            var queueUrl = await queueHelper.GetQueueUrlAsync(configuration.QueueName);
            await sqsService.DeleteMessageAsync(queueUrl, message.ReceiptHandle, token).ConfigureAwait(false);

            LogMessageDeleted(logger, message.MessageId, queueUrl);
        }
        catch (ReceiptHandleIsInvalidException ex)
        {
            LogReceiptHandleIsInvalid(logger, ex, message.ReceiptHandle);
        }
    }

    private static DateTime GetSentDateTime(Message message, TimeSpan clockOffset)
    {
#if NET6_0_OR_GREATER
        var result = DateTime.UnixEpoch.AddMilliseconds(long.Parse(message.Attributes["SentTimestamp"]));
#else
        var result = UnixEpoch.AddMilliseconds(long.Parse(message.Attributes["SentTimestamp"]));
#endif

        // Adjust for clock skew between this endpoint and aws.
        // https://aws.amazon.com/blogs/developer/clock-skew-correction/
        return result + clockOffset;
    }

    private bool IsMessageExpired(Message message, TagList tags)
    {
        var ttl = TimeSpan.MaxValue;

        if (message.Attributes.TryGetValue("MessageTTL", out var sentTtl))
            ttl = TimeSpan.Parse(sentTtl);

        if (ttl == TimeSpan.MaxValue)
            return false;

        var sentDateTime = GetSentDateTime(message, sqsService.Config.ClockOffset);
        var utcNow = DateTime.UtcNow;
        var expiresAt = sentDateTime + ttl;

        if (expiresAt > utcNow)
            return false;

        // Message has expired.
        LogExpiredMessage(logger, message.MessageId, utcNow - expiresAt, expiresAt);

        tags.Add(new KeyValuePair<string, object?>(MeterTags.FailureType, "Message Expired."));
        Meters.TotalExpired.Add(1, tags);

        return true;
    }

    #region Logging Extensions

    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "PurgeQueueInProgressException")]
    private static partial void LogPurgeError(
        ILogger logger,
        PurgeQueueInProgressException ex);

    [LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = "Received {Count} message from queue: {QueueUrl}")]
    private static partial void LogMessageReceived(
        ILogger logger,
        int count,
        string queueUrl);

    [LoggerMessage(EventId = 103, Level = LogLevel.Debug, Message = "Deleted message with the Id: {MessageId} from queue: {QueueUrl}")]
    private static partial void LogMessageDeleted(
        ILogger logger,
        string messageId,
        string queueUrl);

    [LoggerMessage(EventId = 104, Level = LogLevel.Error, Message = "Message receipt handle {ReceiptHandle} no longer valid")]
    private static partial void LogReceiptHandleIsInvalid(
        ILogger logger,
        ReceiptHandleIsInvalidException ex,
        string receiptHandle);

    [LoggerMessage(EventId = 105, Level = LogLevel.Warning, Message = "Discarding expired message with Id {MessageId}, expired {Expired} ago at {ExpiresAt} utc")]
    private static partial void LogExpiredMessage(
        ILogger logger,
        string messageId,
        TimeSpan expired,
        DateTime expiresAt);

    [LoggerMessage(EventId = 106, Level = LogLevel.Debug, Message = "Queue {QueueUrl} purged")]
    private static partial void LogQueuePurge(
        ILogger logger,
        string queueUrl);

    #endregion
}
