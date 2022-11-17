using Amazon.SQS;

using Microsoft.Extensions.Logging;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;

namespace SQS.Extensions.Implementations;

/// <summary>
/// The implementation of <see cref="ISqsMessagePumpFactory"/>
/// </summary>
internal sealed class SqsSqsMessagePumpFactory : ISqsMessagePumpFactory
{
    private readonly IAmazonSQS sqsService;
    private readonly ILoggerFactory loggerFactory;
    private readonly IMessageSerializer messageSerializer;
    private readonly ISqsQueueHelper sqsHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsSqsMessagePumpFactory"/> class.
    /// </summary>
    public SqsSqsMessagePumpFactory(
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
    public async Task<ISqsMessagePump<T>> CreateAsync<T>(MessagePumpConfiguration configuration, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<ILogger<SqsMessagePump<T>>>();

        var pump = new SqsMessagePump<T>(sqsService, logger, configuration, sqsHelper, messageSerializer);
        await pump.InitAsync(cancellationToken).ConfigureAwait(false);

        return pump;
    }
}
