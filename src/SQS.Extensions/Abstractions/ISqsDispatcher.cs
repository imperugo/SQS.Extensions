using Amazon.SQS;
using Amazon.SQS.Model;

namespace SQS.Extensions.Abstractions;

/// <summary>
/// Contract for the client implementation of SQS
/// </summary>
public interface ISqsDispatcher
{
    /// <summary>
    /// The AWS SQS client istance
    /// </summary>
    IAmazonSQS SqsClient { get; }

    /// <summary>
    /// Queue an object into the SQS queue
    /// </summary>
    /// <param name="obj">The message to send.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="T">The type of the expected object.</typeparam>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync<T>(T obj, string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue an object into the SQS queue
    /// </summary>
    /// <param name="obj">The message to send.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="delaySeconds">The delay in seconds before to have the message visible into the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="T">The type of the expected object.</typeparam>
    /// <remarks>
    ///     Specifying the delay doesn't me that the message still in the client for the specified period.
    ///     It will be sent immediately to th queue but saying to SQS to keep it hidden for the specified period.
    /// </remarks>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync<T>(T obj, string queueName, int delaySeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a set of objects into the SQS queue
    /// </summary>
    /// <param name="obj">An array of messages to send into the queue.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="T">The type of the expected object.</typeparam>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync<T>(T[] obj, string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a set of objects into the SQS queue
    /// </summary>
    /// <param name="obj">An array of messages to send into the queue.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="delaySeconds">The delay in seconds before to have the message visible into the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="T">The type of the expected object.</typeparam>
    /// <remarks>
    ///     Specifying the delay doesn't me that the message still in the client for the specified period.
    ///     It will be sent immediately to th queue but saying to SQS to keep it hidden for the specified period.
    /// </remarks>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync<T>(T[] obj, string queueName, int delaySeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue an object into the SQS queue
    /// </summary>
    /// <param name="serializedObject">The object serialized as string.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync(string serializedObject, string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue an serialized object into the SQS queue
    /// </summary>
    /// <param name="serializedObject">The object serialized as string.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="delaySeconds">The delay in seconds before to have the message visible into the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync(string serializedObject, string queueName, int delaySeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a set of serialized objects into the SQS queue
    /// </summary>
    /// <param name="serializedObject">A set of serialized objects as string.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="delaySeconds">The delay in seconds before to have the message visible into the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    ///     Specifying the delay doesn't me that the message still in the client for the specified period.
    ///     It will be sent immediately to th queue but saying to SQS to keep it hidden for the specified period.
    /// </remarks>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync(string[] serializedObject, string queueName, int delaySeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue an object into the SQS queue
    /// </summary>
    /// <param name="request">An instance of <see cref="SendMessageRequest"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync(SendMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a set of object into the SQS queue
    /// </summary>
    /// <param name="requests">An array of <see cref="SendMessageRequest"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the SendMessage service method, as returned by SQS.</returns>
    Task QueueAsync(SendMessageRequest[] requests, CancellationToken cancellationToken = default);
}
