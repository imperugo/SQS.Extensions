using System.Net;

using Amazon.SQS;
using Amazon.SQS.Model;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;
using SQS.Extensions.Implementations;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AWS.SDK.SQS.Extensions.Tests;

public class SqsMessagePumpTests : IAsyncDisposable, IDisposable
{
    private bool isDisposed;
    private readonly IAmazonSQS amazonSqsMock;
    private readonly ILogger loggerMock;
    private readonly ISqsQueueHelper sqsQueueHelperMock;
    private readonly IMessageSerializer messageSerializerMock;
    private readonly MessagePumpConfiguration configuration;

    private readonly SqsMessagePump<string> sut;

    public SqsMessagePumpTests()
    {
        amazonSqsMock = Substitute.For<IAmazonSQS>();
        loggerMock = Substitute.For<ILogger>();
        sqsQueueHelperMock = Substitute.For<ISqsQueueHelper>();
        messageSerializerMock = Substitute.For<IMessageSerializer>();
        configuration = new MessagePumpConfiguration("my-super-queue")
        {
            MaxConcurrentOperation = 10
        };

        sqsQueueHelperMock
            .GetQueueUrlAsync(Arg.Any<string>())
            .Returns(Constants.DEFAULT_TEST_QUEUE_URL);

        sut = new SqsMessagePump<string>(amazonSqsMock, loggerMock, configuration, sqsQueueHelperMock, messageSerializerMock);
    }

    [Fact]
    public async Task Calling_Init_With_PurgeOnStartUp_Flag_Should_Call_Purge_Method_Async()
    {
        configuration.PurgeOnStartup = true;

        await sut.InitAsync();

        await amazonSqsMock.Received(1).PurgeQueueAsync(Constants.DEFAULT_TEST_QUEUE_URL, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Calling_PumpAsync_Should_Call_With_MaxConcurrentOperation_Equal_To_10_Should_Call_ReceiveMessageAsync_Just_One_Time_Async()
    {
        amazonSqsMock.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>()).Returns(new ReceiveMessageResponse { Messages = new List<Message>(), HttpStatusCode = HttpStatusCode.OK });

        await sut.PumpAsync((_, _) => Task.CompletedTask);

        Func<ReceiveMessageRequest?, bool> equal = x => x is { MaxNumberOfMessages: 10, WaitTimeSeconds: 20, QueueUrl: Constants.DEFAULT_TEST_QUEUE_URL };

        await amazonSqsMock.Received(1).ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(
            x => x != null && equal(x)
        ), Arg.Any<CancellationToken>());
    }

    //TODO: Verify expired message
    //TODO: Verify Consume Message
    //TODO: Verify Deleete Message

    public ValueTask DisposeAsync()
    {
        return sut.DisposeAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // The bulk of the clean-up code is implemented in Dispose(bool)
    protected virtual void Dispose(bool disposing)
    {
        if (isDisposed)
            return;

        if (disposing)
        {
            // free managed resources
            amazonSqsMock.Dispose();
        }

        isDisposed = true;
    }

    // NOTE: Leave out the finalizer altogether if this class doesn't
    // own unmanaged resources, but leave the other methods
    // exactly as they are.
    ~SqsMessagePumpTests()
    {
        // Finalizer calls Dispose(false)
        Dispose(false);
    }
}
