using System.Text.Json.Serialization.Metadata;

using Amazon.SQS;
using Amazon.SQS.Model;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Implementations;

using Microsoft.Extensions.Logging;

using Moq;

namespace AWS.SDK.SQS.Extensions.Tests;

public class SqsDispatcherTests
{
    private readonly Mock<IAmazonSQS> amazonSqsMock;
    private readonly Mock<ILogger<SqsDispatcher>> logger = new();
    private readonly SqsDispatcher sut;

    public SqsDispatcherTests()
    {
        amazonSqsMock = new Mock<IAmazonSQS>();
        var serviceProviderMock = new Mock<ISqsQueueHelper>();
        var messageSerializerMock = new Mock<IMessageSerializer>();

        serviceProviderMock
            .Setup(x => x.GetQueueUrl(It.IsAny<string>()))
            .Returns(Constants.DEFAULT_TEST_QUEUE_URL);

        messageSerializerMock
            .Setup(x => x.Serialize(It.IsAny<It.IsAnyType>()))
            .Returns("{}");

        sut = new SqsDispatcher(amazonSqsMock.Object, serviceProviderMock.Object, logger.Object, messageSerializerMock.Object);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(13)]
    [InlineData(57)]
    [InlineData(704)]
    public async Task Calling_QueueAsync_With_Array_Of_Object_Should_Batch_Correctly_Async(int numberOfMessages)
    {
        var messages = new MockRequest[numberOfMessages];

        for (var i = 0; i < numberOfMessages; i++)
            messages[i] = new MockRequest { Message = "Ciao " + i };

        var totalNumberOfRequest = (numberOfMessages + 10 - 1) / 10;

        await sut.QueueAsync(messages, "my-super-queue");

        amazonSqsMock.Verify(x => x.SendMessageBatchAsync(It.Is<string>(x => x == Constants.DEFAULT_TEST_QUEUE_URL), It.IsAny<List<SendMessageBatchRequestEntry>>(), It.IsAny<CancellationToken>()), Times.Exactly(totalNumberOfRequest));
    }

    [Fact]
    public async Task Calling_QueueAsync_With_Only_QueueName_And_Serialized_Object_Should_Add_The_Right_Queue_Async()
    {
        const string messageBody = "My Message Body";

        await sut.QueueAsync(messageBody, "my-super-queue", 10);

        amazonSqsMock.Verify(x => x.SendMessageAsync(It.Is<SendMessageRequest>(x => x.QueueUrl == Constants.DEFAULT_TEST_QUEUE_URL && x.DelaySeconds == 10 && x.MessageBody == messageBody), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calling_QueueAsync_With_Only_QueueName_Should_Add_The_Right_Queue_Async()
    {
        var request = new SendMessageRequest();
        request.QueueUrl = "my-super-queue";

        await sut.QueueAsync(request, default);

        amazonSqsMock.Verify(x => x.SendMessageAsync(It.Is<SendMessageRequest>(x => x.QueueUrl == Constants.DEFAULT_TEST_QUEUE_URL), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calling_QueueAsync_With_Full_QueueName_But_Without_Prefix_Should_Add_The_Right_Queue_Async()
    {
        var request = new SendMessageRequest();
        request.QueueUrl = "my-super-queue";

        await sut.QueueAsync(request);

        amazonSqsMock.Verify(x => x.SendMessageAsync(It.Is<SendMessageRequest>(x => x.QueueUrl == Constants.DEFAULT_TEST_QUEUE_URL), It.IsAny<CancellationToken>()), Times.Once);
    }

    private class MockRequest
    {
        public string? Message { get; set; }
    }
}
