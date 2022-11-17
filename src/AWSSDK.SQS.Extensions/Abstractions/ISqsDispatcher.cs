using Amazon.SQS;
using Amazon.SQS.Model;

namespace AWSSDK.SQS.Extensions.Abstractions;

public interface ISqsDispatcher
{
    IAmazonSQS SqsClient { get; }
    Task QueueAsync<T>(T obj, string queueName, CancellationToken cancellationToken = default);
    Task QueueAsync<T>(T obj, string queueName, int delaySeconds, CancellationToken cancellationToken = default);
    Task QueueAsync<T>(T[] obj, string queueName, CancellationToken cancellationToken = default);
    Task QueueAsync<T>(T[] obj, string queueName, int delaySeconds, CancellationToken cancellationToken = default);
    Task QueueAsync(string serializedObject, string queueName, CancellationToken cancellationToken = default);
    Task QueueAsync(string serializedObject, string queueName, int delaySeconds, CancellationToken cancellationToken = default);
    Task QueueAsync(string[] serializedObject, string queueName, int delaySeconds, CancellationToken cancellationToken = default);
    Task QueueAsync(SendMessageRequest request, CancellationToken cancellationToken = default);
    Task QueueAsync(SendMessageRequest[] requests, CancellationToken cancellationToken = default);
}
