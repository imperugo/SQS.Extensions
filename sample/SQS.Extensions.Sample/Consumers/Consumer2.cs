// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;

namespace SQS.Extensions.Sample.Consumers;

internal sealed class Consumer2 : SqsHostedService<MySqsMessage>
{
    public Consumer2(
        ILogger<Consumer2> logger,
        ISqsMessagePumpFactory messagePumpFactory)
            : base(logger, messagePumpFactory)
    {
    }

    protected override Func<MySqsMessage?, MessageContext, CancellationToken, Task> ProcessMessageFunc => ConsumeMessageAsync;

    protected override MessagePumpConfiguration MessagePumpConfiguration =>
        new (Constants.QUEUE_NAME_2)
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
