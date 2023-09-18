// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;

using Amazon.SQS;
using Amazon.SQS.Model;

using Microsoft.Extensions.Logging;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;
using SQS.Extensions.OpenTelemetry.Metrics;

namespace SQS.Extensions.Implementations.MessagePump;

internal abstract partial class MessagePumpBase<TMessage> :
    ISqsMessagePumpBase,
    IAsyncDisposable
{
    private readonly MessagePumpConfiguration configuration;
    private readonly ISqsQueueHelper queueHelper;

#if NET6_0_OR_GREATER
#else
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
#endif

    protected readonly CancellationTokenSource MessagePumpCancellationTokenSource = new();
    protected readonly CancellationTokenSource MessageProcessingCancellationTokenSource = new();

    protected TagList TagList;
    protected readonly Task[] PumpTasks;
    protected readonly ILogger Logger;
    protected readonly int NumberOfPumps;
    protected readonly IAmazonSQS SqsService;
    protected string QueueUrl = string.Empty;
    protected readonly int NumberOfMessagesToFetch;
    protected IMessageSerializer MessageSerializer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePumpBase{T}"/> class.
    /// </summary>
    public MessagePumpBase(
        IAmazonSQS sqsService,
        ILogger logger,
        MessagePumpConfiguration configuration,
        ISqsQueueHelper queueHelper,
        IMessageSerializer messageSerializer)
    {
        this.SqsService = sqsService;
        this.Logger = logger;
        this.configuration = configuration;
        this.queueHelper = queueHelper;
        this.MessageSerializer = messageSerializer;

        if (configuration.MaxConcurrentOperation <= 10)
        {
            NumberOfPumps = 1;
            NumberOfMessagesToFetch = configuration.MaxConcurrentOperation;
        }
        else
        {
            // 10 is a limit of SQS reason why I've to scale with the pump
            NumberOfMessagesToFetch = 10;
            NumberOfPumps = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(configuration.MaxConcurrentOperation) / NumberOfMessagesToFetch));
        }

        PumpTasks = new Task[NumberOfPumps];

        TagList = new TagList(new KeyValuePair<string, object>[]
            {
                new(MeterTags.QueueName, configuration.QueueName),
                new(MeterTags.MessageType, typeof(TMessage))
            }
            .AsSpan()!);
    }

    /// <inheritdoc/>
    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(QueueUrl))
        {
            QueueUrl = await queueHelper.GetQueueUrlAsync(configuration.QueueName).ConfigureAwait(false);
            LogPumpInitialized(Logger, QueueUrl, NumberOfPumps, NumberOfMessagesToFetch);
        }

        if (!configuration.PurgeOnStartup)
            return;

        try
        {
            await SqsService.PurgeQueueAsync(QueueUrl, cancellationToken).ConfigureAwait(false);

            LogQueuePurge(Logger, QueueUrl);
        }
        catch (PurgeQueueInProgressException ex)
        {
            LogPurgeError(Logger, ex);
        }
    }

    /// <inheritdoc/>
    public async Task StopPumpAsync(CancellationToken cancellationToken = default)
    {
        MessagePumpCancellationTokenSource.Cancel();

#if NET6_0_OR_GREATER
        await using (cancellationToken.Register(() => MessageProcessingCancellationTokenSource.Cancel()))
#else
        using (cancellationToken.Register(() => MessageProcessingCancellationTokenSource.Cancel()))
#endif

        await Task.WhenAll(PumpTasks).ConfigureAwait(false);

        MessagePumpCancellationTokenSource.Dispose();
        MessageProcessingCancellationTokenSource.Dispose();
    }

    protected MessageContext GetContext(Message message)
    {
        var messageContext = new MessageContext(message);

        if (message.Attributes.TryGetValue(MessageSystemAttributeName.ApproximateReceiveCount, out var receiveCount))
            messageContext.RetryCount = int.Parse(receiveCount);

        return messageContext;
    }

    protected bool IsMessageExpired(Message message)
    {
        var ttl = TimeSpan.MaxValue;

        if (message.Attributes.TryGetValue("MessageTTL", out var sentTtl))
            ttl = TimeSpan.Parse(sentTtl);

        if (ttl == TimeSpan.MaxValue)
            return false;

        var sentDateTime = GetSentDateTime(message, SqsService.Config.ClockOffset);
        var utcNow = DateTime.UtcNow;
        var expiresAt = sentDateTime + ttl;

        if (expiresAt > utcNow)
            return false;

        // Message has expired.
        LogExpiredMessage(Logger, message.MessageId, utcNow - expiresAt, expiresAt);

        TagList.Add(new KeyValuePair<string, object?>(MeterTags.FailureType, "Message Expired."));
        Meters.TotalExpired.Add(1, TagList);

        return true;
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

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopPumpAsync().ConfigureAwait(false);
    }

    #region Logging

    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "PurgeQueueInProgressException")]
    protected static partial void LogPurgeError(
        ILogger logger,
        PurgeQueueInProgressException ex);

    [LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = "Received {Count} message from queue: {QueueUrl}")]
    protected static partial void LogMessageReceived(
        ILogger logger,
        int count,
        string queueUrl);

    [LoggerMessage(EventId = 103, Level = LogLevel.Debug, Message = "Deleted message with the Id: {MessageId} from queue: {QueueUrl}")]
    protected static partial void LogMessageDeleted(
        ILogger logger,
        string messageId,
        string queueUrl);

    [LoggerMessage(EventId = 104, Level = LogLevel.Debug, Message = "Deleted {MessageCount} message from queue: {QueueUrl}")]
    protected static partial void LogMessagesDeleted(
        ILogger logger,
        int messageCount,
        string queueUrl);

    [LoggerMessage(EventId = 105, Level = LogLevel.Warning, Message = "Discarding expired message with Id {MessageId}, expired {Expired} ago at {ExpiresAt} utc")]
    protected static partial void LogExpiredMessage(
        ILogger logger,
        string messageId,
        TimeSpan expired,
        DateTime expiresAt);

    [LoggerMessage(EventId = 106, Level = LogLevel.Error, Message = "Message receipt handle {ReceiptHandle} no longer valid")]
    protected static partial void LogReceiptHandleIsInvalid(
        ILogger logger,
        ReceiptHandleIsInvalidException ex,
        string receiptHandle);

    [LoggerMessage(EventId = 107, Level = LogLevel.Debug, Message = "Queue {QueueUrl} purged")]
    protected static partial void LogQueuePurge(
        ILogger logger,
        string queueUrl);

    [LoggerMessage(EventId = 108, Level = LogLevel.Debug, Message = "Message Pump for queue: {QueueUrl} initialized. NumberOrPumps: {NumberOfPumps}, NumberOfMessagesToFetch: {NumberOfMessagesToFetch}")]
    protected static partial void LogPumpInitialized(
        ILogger logger,
        string queueUrl,
        int numberOfPumps,
        int numberOfMessagesToFetch);

    #endregion
}
