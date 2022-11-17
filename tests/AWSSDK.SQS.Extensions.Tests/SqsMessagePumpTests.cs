using System.Net;

using Amazon.SQS;
using Amazon.SQS.Model;

using AWSSDK.SQS.Extensions.Abstractions;
using AWSSDK.SQS.Extensions.Configurations;
using AWSSDK.SQS.Extensions.Implementations;

using Microsoft.Extensions.Logging;

using Moq;

namespace AWSSDK.SQS.Extensions.Tests;

public class SqsMessagePumpTests : IAsyncDisposable
{
    private readonly Mock<IAmazonSQS> amazonSqsMock;
    private readonly Mock<ILogger> loggerMock;
    private readonly Mock<ISqsQueueHelper> sqsQueueHelperMock;
    private readonly Mock<IMessageSerializer> messageSerializerMock;
    private readonly MessagePumpConfiguration configuration;

    private readonly SqsMessagePump<string> sut;

    public SqsMessagePumpTests()
    {
        amazonSqsMock = new Mock<IAmazonSQS>();
        loggerMock = new Mock<ILogger>();
        sqsQueueHelperMock = new Mock<ISqsQueueHelper>();
        messageSerializerMock = new Mock<IMessageSerializer>();
        configuration = new MessagePumpConfiguration("my-super-queue")
        {
            MaxConcurrentOperation = 10
        };

        sqsQueueHelperMock
            .Setup(x => x.GetQueueUrl(It.IsAny<string>()))
            .Returns(Constants.DEFAULT_TEST_QUEUE_URL);

        sut = new SqsMessagePump<string>(amazonSqsMock.Object, loggerMock.Object, configuration, sqsQueueHelperMock.Object, messageSerializerMock.Object);
    }

    [Fact]
    public async Task Calling_Init_With_PurgeOnStartUp_Flag_Should_Call_Purge_Method_Async()
    {
        configuration.PurgeOnStartup = true;

        await sut.InitAsync();

        amazonSqsMock
            .Verify(x => x.PurgeQueueAsync(Constants.DEFAULT_TEST_QUEUE_URL, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calling_PumpAsync_Should_Call_With_MaxConcurrentOperation_Equal_To_10_Should_Call_ReceiveMessageAsync_Just_One_Time_Async()
    {
        amazonSqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>(), HttpStatusCode = HttpStatusCode.OK });

        await sut.PumpAsync((_, _) => Task.CompletedTask);

        Func<ReceiveMessageRequest?, bool> equal = x => x is { MaxNumberOfMessages: 10, WaitTimeSeconds: 20, QueueUrl: Constants.DEFAULT_TEST_QUEUE_URL };

        amazonSqsMock.Verify(x => x.ReceiveMessageAsync(It.Is<ReceiveMessageRequest?>(x => equal(x)), It.IsAny<CancellationToken>()), Times.Once);
    }

    //TODO: Verify expired message
    //TODO: Verify Consume Message
    //TODO: Verify Deleete Message

    public ValueTask DisposeAsync()
    {
        return sut.DisposeAsync();
    }
}
