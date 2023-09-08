// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Amazon.SQS;
using Amazon.SQS.Model;

using Microsoft.Extensions.Logging;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;
using SQS.Extensions.OpenTelemetry.Metrics;

namespace SQS.Extensions.Implementations.MessagePump;

internal class BulkMessagePump<TMessage>
    : MessagePumpBase<TMessage>,
        ISqsBulkMessagePump<TMessage> where TMessage : notnull
{
    public BulkMessagePump(
        IAmazonSQS sqsService,
        ILogger logger,
        MessagePumpConfiguration configuration,
        ISqsQueueHelper queueHelper,
        IMessageSerializer messageSerializer)
            : base(sqsService, logger, configuration, queueHelper, messageSerializer)
    {
    }

    public async Task PumpAsync(Func<Dictionary<TMessage, MessageContext>, CancellationToken, Task> processMessageAsync, CancellationToken cancellationToken = default)
    {
        var receiveMessagesRequest = new ReceiveMessageRequest
        {
            MaxNumberOfMessages = NUMBER_OF_MESSAGES_TO_FETCH,
            QueueUrl = QueueUrl,
            WaitTimeSeconds = 20,
            AttributeNames = new List<string>
            {
                "SentTimestamp",
                "MessageTTL"
            }
        };

#if NET6_0_OR_GREATER
        var emptyArray = new object[NumberOfPumps];
        await Parallel.ForEachAsync(emptyArray, cancellationToken, async (_, token) => await ConsumeMessagesAsync(processMessageAsync, receiveMessagesRequest, token).ConfigureAwait(false)).ConfigureAwait(false);
#else
        for (var i = 0; i < NumberOfPumps; i++)
            PumpTasks[i] = ConsumeMessagesAsync(processMessageAsync, receiveMessagesRequest, cancellationToken);

        await Task.WhenAll(PumpTasks).ConfigureAwait(false);
#endif
    }

    private async Task ConsumeMessagesAsync(
        Func<Dictionary<TMessage, MessageContext>, CancellationToken, Task> processMessagesAsync,
        ReceiveMessageRequest messageRequest,
        CancellationToken cancellationToken)
    {
        var receivedMessages = await SqsService.ReceiveMessageAsync(messageRequest, cancellationToken).ConfigureAwait(false);

        LogMessageReceived(Logger, receivedMessages.Messages.Count, QueueUrl);

        Meters.TotalFetched.Add(receivedMessages.Messages.Count, TagList);
        await ProcessMessagesAsync(processMessagesAsync, receivedMessages.Messages, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessMessagesAsync(
        Func<Dictionary<TMessage, MessageContext>, CancellationToken, Task> processMessageAsync,
        IList<Message> messages,
        CancellationToken cancellationToken)
    {
        var messagesToBeParsed = new Dictionary<TMessage, MessageContext>(messages.Count);

        foreach (var message in messages)
        {
            if (IsMessageExpired(message))
                continue;

            var msg = MessageSerializer.Deserialize<TMessage>(message.Body);
            var ctx = GetContext(message);
            messagesToBeParsed.Add(msg!, ctx);
        }

        try
        {
            await processMessageAsync(messagesToBeParsed, MessageProcessingCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            TagList.Add(new KeyValuePair<string, object?>(MeterTags.FailureType, ex.GetType()));
            Meters.TotalFailures.Add(messagesToBeParsed.Count, TagList);
            throw;
        }

        // Always delete the message from the queue.
        // If processing failed, the onError handler will have moved the message
        // to a retry queue.
        await DeleteMessageAndBodyIfRequiredAsync(messages, cancellationToken).ConfigureAwait(false);
        Meters.TotalDeleted.Add(messages.Count, TagList);
    }

    private async Task DeleteMessageAndBodyIfRequiredAsync(IList<Message> messages,  CancellationToken token)
    {
        var entries = new List<DeleteMessageBatchRequestEntry>();

        foreach (var msg in messages)
        {
            entries.Add(new DeleteMessageBatchRequestEntry()
            {
                Id = msg.MessageId,
                ReceiptHandle = msg.ReceiptHandle
            });
        }

        await SqsService.DeleteMessageBatchAsync(QueueUrl, entries, token).ConfigureAwait(false);

        LogMessagesDeleted(Logger, messages.Count, QueueUrl);
    }
}
