using System.Net;

using Amazon.SQS;
using Amazon.SQS.Model;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;

using Microsoft.Extensions.Logging;

using NSubstitute;

using SQS.Extensions;
using SQS.Extensions.Implementations.MessagePump;

namespace AWS.SDK.SQS.Extensions.Tests;

public class SqsMessagePumpTests : IAsyncDisposable, IDisposable
{
    private bool isDisposed;
    private readonly IAmazonSQS amazonSqsMock;
    private readonly ILogger loggerMock;
    private readonly ISqsQueueHelper sqsQueueHelperMock;
    private readonly IMessageSerializer messageSerializerMock;
    private readonly MessagePumpConfiguration configuration;

    private readonly SingleMessagePump<string> sut;

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

        sut = new SingleMessagePump<string>(amazonSqsMock, loggerMock, configuration, sqsQueueHelperMock, messageSerializerMock);
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

        await sut.InitAsync();
        await sut.PumpAsync((_, _, _) => Task.CompletedTask);

        Func<ReceiveMessageRequest?, bool> equal = x => x is { MaxNumberOfMessages: 10, WaitTimeSeconds: 20, QueueUrl: Constants.DEFAULT_TEST_QUEUE_URL };

        await amazonSqsMock.Received(1).ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(
            x => x != null && equal(x)
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Calling_PumpAsync_With_60_Messages_Should_Call_Custom_Function_60_Times_Async()
    {
        var messages = new List<Message>();

        // 10 is the max number of messages that SQS can return on a single request
        for (var i = 0; i < 10; i++)
            messages.Add(new Message());

        var configuration = new MessagePumpConfiguration("my-super-queue")
        {
            MaxConcurrentOperation = 60
        };

        amazonSqsMock
            .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ReceiveMessageResponse { Messages = messages, HttpStatusCode = HttpStatusCode.OK });

        var scopedSut = new SingleMessagePump<string>(amazonSqsMock, loggerMock, configuration, sqsQueueHelperMock, messageSerializerMock);

        var customFunctionMock = Substitute.For<Func<string?, MessageContext, CancellationToken, Task>>();

        customFunctionMock
            .When(x => x.Invoke(Arg.Any<string>(), Arg.Any<MessageContext>(), Arg.Any<CancellationToken>()));

        // Act
        await scopedSut.InitAsync();
        await scopedSut.PumpAsync(customFunctionMock, CancellationToken.None);

        // Assert
        await amazonSqsMock
            .Received(6)
            .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>());

        await customFunctionMock
            .Received(60)
            .Invoke(Arg.Any<string>(), Arg.Any<MessageContext>(), Arg.Any<CancellationToken>());
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
