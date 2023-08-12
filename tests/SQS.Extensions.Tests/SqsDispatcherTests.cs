using System.Text.Json.Serialization.Metadata;

using Amazon.SQS;
using Amazon.SQS.Model;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Implementations;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AWS.SDK.SQS.Extensions.Tests;

public class SqsDispatcherTests
{
    private readonly IAmazonSQS amazonSqsMock;
    private readonly ILogger<SqsDispatcher> logger = Substitute.For<ILogger<SqsDispatcher>>();
    private readonly SqsDispatcher sut;

    public SqsDispatcherTests()
    {
        amazonSqsMock = Substitute.For<IAmazonSQS>();
        var serviceProviderMock = Substitute.For<ISqsQueueHelper>();
        var messageSerializerMock = Substitute.For<IMessageSerializer>();

        serviceProviderMock
            .GetQueueUrlAsync(Arg.Any<string>())
            .Returns(Constants.DEFAULT_TEST_QUEUE_URL);

        messageSerializerMock
            .Serialize(Arg.Any<object>())
            .Returns("{}");

        sut = new SqsDispatcher(amazonSqsMock, serviceProviderMock, logger, messageSerializerMock);
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

        await sut.QueueBatchAsync(messages, "my-super-queue");

        await amazonSqsMock.Received(totalNumberOfRequest).SendMessageBatchAsync(
            Arg.Is<string>(x => x == Constants.DEFAULT_TEST_QUEUE_URL),
            Arg.Any<List<SendMessageBatchRequestEntry>>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Calling_QueueAsync_With_Only_QueueName_Should_Add_The_Right_Queue_Async()
    {
        var request = new SendMessageRequest();
        request.QueueUrl = "my-super-queue";

        await sut.QueueAsync(request, default);

        await amazonSqsMock.Received(1).SendMessageAsync(
            Arg.Is<SendMessageRequest>(x => x.QueueUrl == Constants.DEFAULT_TEST_QUEUE_URL),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Calling_QueueAsync_With_Full_QueueName_But_Without_Prefix_Should_Add_The_Right_Queue_Async()
    {
        var request = new SendMessageRequest();
        request.QueueUrl = "my-super-queue";

        await sut.QueueAsync(request);

        await amazonSqsMock.Received(1).SendMessageAsync(
            Arg.Is<SendMessageRequest>(x => x.QueueUrl == Constants.DEFAULT_TEST_QUEUE_URL),
            Arg.Any<CancellationToken>()
        );
    }

    private class MockRequest
    {
        public string? Message { get; set; }
    }
}
