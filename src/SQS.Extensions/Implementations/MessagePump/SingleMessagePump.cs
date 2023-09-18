// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Amazon.SQS;
using Amazon.SQS.Model;

using Microsoft.Extensions.Logging;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;
using SQS.Extensions.OpenTelemetry.Metrics;

namespace SQS.Extensions.Implementations.MessagePump;

internal sealed class SingleMessagePump<TMessage>
    : MessagePumpBase<TMessage>,
        ISqsMessagePump<TMessage> where TMessage : notnull
{
    public SingleMessagePump(
        IAmazonSQS sqsService,
        ILogger logger,
        MessagePumpConfiguration configuration,
        ISqsQueueHelper queueHelper,
        IMessageSerializer messageSerializer)
        : base(sqsService, logger, configuration, queueHelper, messageSerializer)
    {
    }

    public async Task PumpAsync(Func<TMessage?, MessageContext, CancellationToken, Task> processMessageAsync, CancellationToken cancellationToken = default)
    {
        var receiveMessagesRequest = new ReceiveMessageRequest
        {
            MaxNumberOfMessages = NumberOfMessagesToFetch,
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
        Func<TMessage?, MessageContext, CancellationToken, Task> processMessageAsync,
        ReceiveMessageRequest messageRequest,
        CancellationToken cancellationToken)
    {
        var receivedMessages = await SqsService.ReceiveMessageAsync(messageRequest, cancellationToken).ConfigureAwait(false);

        LogMessageReceived(Logger, receivedMessages.Messages.Count, QueueUrl);

        Meters.TotalFetched.Add(receivedMessages.Messages.Count, TagList);

#if NET6_0_OR_GREATER
        await Parallel.ForEachAsync(receivedMessages.Messages, cancellationToken, async (message, token) =>
        {
            var ctx = GetContext(message);
            await ProcessMessageAsync(processMessageAsync, message, ctx, token).ConfigureAwait(false);
        });
#else
        var tasks = new Task[receivedMessages.Messages.Count];

        for (var i = 0; i < receivedMessages.Messages.Count; i++)
        {
            var message = receivedMessages.Messages[i];
            var ctx = GetContext(message);
            tasks[i] = ProcessMessageAsync(processMessageAsync, message, ctx, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif

    }

    private async Task ProcessMessageAsync(
        Func<TMessage?, MessageContext, CancellationToken, Task> processMessageAsync,
        Message message,
        MessageContext context,
        CancellationToken cancellationToken)
    {
        if (!IsMessageExpired(message))
        {
            try
            {
                await processMessageAsync(MessageSerializer.Deserialize<TMessage>(message.Body), context, MessageProcessingCancellationTokenSource.Token).ConfigureAwait(false);
                Meters.TotalProcessedSuccessfully.Add(1, TagList);
            }
            catch (Exception ex)
            {
                TagList.Add(new KeyValuePair<string, object?>(MeterTags.FailureType, ex.GetType()));
                Meters.TotalFailures.Add(1, TagList);
                throw;
            }
        }

        // Always delete the message from the queue.
        // If processing failed, the onError handler will have moved the message
        // to a retry queue.
        await DeleteMessageAndBodyIfRequiredAsync(message, cancellationToken).ConfigureAwait(false);
        Meters.TotalDeleted.Add(1, TagList);
    }

    private async Task DeleteMessageAndBodyIfRequiredAsync(Message message,  CancellationToken token)
    {
        try
        {
            await SqsService.DeleteMessageAsync(QueueUrl, message.ReceiptHandle, token).ConfigureAwait(false);

            LogMessageDeleted(Logger, message.MessageId, QueueUrl);
        }
        catch (ReceiptHandleIsInvalidException ex)
        {
            LogReceiptHandleIsInvalid(Logger, ex, message.ReceiptHandle);
        }
    }
}
