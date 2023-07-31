namespace SQS.Extensions.Abstractions;

/// <summary>
/// The contract used for retrieve the queue url from the queue name
/// </summary>
public interface ISqsQueueHelper
{
    /// <summary>
    /// Normalize the queue name
    /// </summary>
    /// <param name="queueName">The name of the queue.</param>
    /// <returns>The url of the queue</returns>
    /// <example>
    ///    "https://sqs.eu-central-1.amazonaws.com/775704350706/develop-queue-name"
    /// </example>
    string GetQueueUrl(string queueName);

    /// <summary>
    /// Gets the length of the specified queue.
    /// </summary>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The length of the queue.</returns>
    Task<long> GetQueueLengthAsync(string queueName, CancellationToken cancellationToken = default);
}
