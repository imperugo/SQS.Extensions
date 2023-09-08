using Amazon.SQS;

using Microsoft.Extensions.Logging;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;

namespace SQS.Extensions.Implementations.MessagePump;

/// <summary>
/// The implementation of <see cref="ISqsMessagePumpFactory"/>
/// </summary>
internal sealed class MessagePumpFactory : ISqsMessagePumpFactory
{
    private readonly IAmazonSQS sqsService;
    private readonly ILoggerFactory loggerFactory;
    private readonly IMessageSerializer messageSerializer;
    private readonly ISqsQueueHelper sqsHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePumpFactory"/> class.
    /// </summary>
    public MessagePumpFactory(
        IAmazonSQS sqsService,
        ILoggerFactory loggerFactory,
        IMessageSerializer messageSerializer,
        ISqsQueueHelper sqsHelper)
    {
        this.sqsService = sqsService;
        this.loggerFactory = loggerFactory;
        this.messageSerializer = messageSerializer;
        this.sqsHelper = sqsHelper;
    }

    /// <inheritdoc/>
    public async Task<ISqsMessagePump<TMessage>> CreateMessagePumpAsync<TMessage>(MessagePumpConfiguration configuration, CancellationToken cancellationToken)
        where TMessage : notnull
    {
        var logger = loggerFactory.CreateLogger<ILogger<SingleMessagePump<TMessage>>>();

        var pump = new SingleMessagePump<TMessage>(sqsService, logger, configuration, sqsHelper, messageSerializer);
        await pump.InitAsync(cancellationToken).ConfigureAwait(false);

        return pump;
    }

    public async Task<ISqsBulkMessagePump<TMessage>> CreateBulkMessagePumpAsync<TMessage>(MessagePumpConfiguration configuration, CancellationToken cancellationToken) where TMessage : notnull
    {
        var logger = loggerFactory.CreateLogger<ILogger<SingleMessagePump<TMessage>>>();

        var pump = new BulkMessagePump<TMessage>(sqsService, logger, configuration, sqsHelper, messageSerializer);
        await pump.InitAsync(cancellationToken).ConfigureAwait(false);

        return pump;
    }
}
