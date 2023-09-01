using Amazon.SQS;
using Amazon.SQS.Model;

using SQS.Extensions.Extensions;

using Microsoft.Extensions.Logging;

using SQS.Extensions.Abstractions;

namespace SQS.Extensions.Implementations;

/// <summary>
/// Jil implementation of <see cref="ISqsDispatcher"/>
/// </summary>
internal sealed partial class SqsDispatcher : ISqsDispatcher
{
    private readonly ISqsQueueHelper sqsHelper;
    private readonly ILogger<SqsDispatcher> logger;
    private readonly IMessageSerializer messageSerializer;

    public IAmazonSQS SqsClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsDispatcher"/> class.
    /// </summary>
    public SqsDispatcher(
        IAmazonSQS sqsService,
        ISqsQueueHelper sqsHelper,
        ILogger<SqsDispatcher> logger,
        IMessageSerializer messageSerializer)
    {
        SqsClient = sqsService ?? throw new ArgumentNullException(nameof(sqsService));

        this.sqsHelper = sqsHelper;
        this.logger = logger;
        this.messageSerializer = messageSerializer;
    }

    /// <inheritdoc/>
    public async Task QueueAsync<T>(T obj, string queueName, int delaySeconds = 0, Dictionary<string, string>? messageAttributes = null, Func<T, string>? serialize = null, CancellationToken cancellationToken = default)
    {
        var queueUrl = await sqsHelper.GetQueueUrlAsync(queueName);

        var serializedObject = serialize != null
            ? serialize(obj)
            : messageSerializer.Serialize(obj);

        var request = CreateSendMessageRequest(serializedObject, queueUrl, delaySeconds, messageAttributes);

        LogPushingMessage(logger, queueName);

        await SqsClient.SendMessageAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task QueueBatchAsync<T>(IList<T> obj, string queueName, int delaySeconds = 0, int maxNumberOfMessagesForBatch = 10, Dictionary<string, string>? messageAttributes = null, Func<T, string>? serialize = null, CancellationToken cancellationToken = default)
    {
        var requests = new SendMessageRequest[obj.Count];
        var queueUrl = await sqsHelper.GetQueueUrlAsync(queueName);

        for (var i = 0; i < obj.Count; i++)
        {
            var serializedObject = serialize != null
                ? serialize(obj[i])
                : messageSerializer.Serialize(obj[i]);

            requests[i] = CreateSendMessageRequest(serializedObject, queueUrl, delaySeconds, messageAttributes);
        }

        await QueueBatchAsync(requests, maxNumberOfMessagesForBatch, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task QueueAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        // This is needed in case the caller will add the queue name instead of queue url
        request.QueueUrl = await sqsHelper.GetQueueUrlAsync(request.QueueUrl);

        await SqsClient.SendMessageAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task QueueBatchAsync(IList<SendMessageRequest> requests, int maxNumberOfMessagesForBatch = 10, CancellationToken cancellationToken = default)
    {
        // Li gruppo per coda
        foreach (var group in requests.GroupBy(x => x.QueueUrl))
        {
            var queueUrl = group.First().QueueUrl;

            // Li gruppo per 10 che è il massimo numero di messaggi che si possono inviare in una singola richiesta
#if NET6_0 || NET7_0
            await Parallel.ForEachAsync(group.Chunk(maxNumberOfMessagesForBatch), cancellationToken, async (messages, token) =>
            {
                var entries = new List<SendMessageBatchRequestEntry>(maxNumberOfMessagesForBatch);

                foreach (var message in messages)
                {
                    var entry = new SendMessageBatchRequestEntry();
                    entry.Id = Guid.NewGuid().ToString("N");
                    entry.MessageBody = message.MessageBody;
                    entry.DelaySeconds = message.DelaySeconds;
                    entry.MessageAttributes = message.MessageAttributes;
                    entries.Add(entry);
                }

                await SqsClient.SendMessageBatchAsync(queueUrl, entries, token).ConfigureAwait(false);
            })
                .ConfigureAwait(false);
#else
            var tasks = new List<Task>(requests.Count/ maxNumberOfMessagesForBatch);

            foreach (var messages in group.ToList().Split(maxNumberOfMessagesForBatch))
            {
                var entries = new List<SendMessageBatchRequestEntry>(maxNumberOfMessagesForBatch);

                foreach (var message in messages)
                {
                    var entry = new SendMessageBatchRequestEntry();
                    entry.Id = Guid.NewGuid().ToString("N");
                    entry.MessageBody = message.MessageBody;
                    entry.DelaySeconds = message.DelaySeconds;
                    entries.Add(entry);
                }

                tasks.Add(SqsClient.SendMessageBatchAsync(queueUrl, entries, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
        }
    }

    private SendMessageRequest CreateSendMessageRequest(string serializedObject, string queueName, int delaySeconds = 0, Dictionary<string, string>? messageAttributes = null)
    {
        var req = new SendMessageRequest
        {
            QueueUrl = queueName,
            MessageBody = serializedObject,
            DelaySeconds = delaySeconds
        };

        if(messageAttributes is not null)
        {
            foreach (var (key, value) in messageAttributes)
                req.MessageAttributes.Add(key, new MessageAttributeValue { DataType = "String", StringValue = value });
        }

        return req;
    }

    [LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = "Pushing message into SQS Queue: {QueueName}")]
    private static partial void LogPushingMessage(
        ILogger logger,
        string queueName);
}
