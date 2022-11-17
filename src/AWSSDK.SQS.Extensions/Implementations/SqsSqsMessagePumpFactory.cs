using Amazon.SQS;

using AWSSDK.SQS.Extensions.Abstractions;
using AWSSDK.SQS.Extensions.Configurations;

using Microsoft.Extensions.Logging;

namespace AWSSDK.SQS.Extensions.Implementations;

internal sealed class SqsSqsMessagePumpFactory : ISqsMessagePumpFactory
{
    private readonly IAmazonSQS sqsService;
    private readonly ILoggerFactory loggerFactory;
    private readonly IMessageSerializer messageSerializer;
    private readonly ISqsQueueHelper sqsHelper;

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

    public async Task<ISqsMessagePump<T>> CreateAsync<T>(MessagePumpConfiguration configuration, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<ILogger<SqsMessagePump<T>>>();

        var pump = new SqsMessagePump<T>(sqsService, logger, configuration, sqsHelper, messageSerializer);
        await pump.InitAsync(cancellationToken).ConfigureAwait(false);

        return pump;
    }
}
