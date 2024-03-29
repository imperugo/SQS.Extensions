// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;
using SQS.Extensions.HostedServices;

namespace SQS.Extensions.Sample.Consumers;

internal sealed class Consumer1 : SqsHostedService<MySqsMessage>
{
    public Consumer1(
        ILogger<Consumer1> logger,
        ISqsMessagePumpFactory messagePumpFactory)
            : base(logger, messagePumpFactory)
    {
    }

    protected override Func<MySqsMessage?, MessageContext, CancellationToken, Task> ProcessMessageFunc => ConsumeMessageAsync;

    protected override MessagePumpConfiguration MessagePumpConfiguration =>
        new (Constants.QUEUE_NAME_1)
        {
            // waiting time between calls to SQS.
            BatchDelay = TimeSpan.FromSeconds(10),

            // the max number of concurrent operations
            MaxConcurrentOperation = 10,

            // if true every time the app start cleans the queue
            // helpful for testing
            PurgeOnStartup = true
        };

    private Task ConsumeMessageAsync(MySqsMessage? message, MessageContext context, CancellationToken cancellationToken)
    {
        // Do your staff here

        return Task.CompletedTask;
    }
}
