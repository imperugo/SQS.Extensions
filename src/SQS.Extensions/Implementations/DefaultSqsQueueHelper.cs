using System.Collections.Concurrent;

using Amazon.SQS;
using Amazon.SQS.Model;

using SQS.Extensions.Abstractions;
using SQS.Extensions.Configurations;

namespace SQS.Extensions.Implementations;

/// <summary>
/// Default implementation of <see cref="ISqsQueueHelper"/>
/// </summary>
public class DefaultSqsQueueHelper : ISqsQueueHelper
{
    private static readonly ConcurrentDictionary<string, string> queueUrlsCache = new ();
    private readonly AwsConfiguration awsConfiguration;
    private readonly IAmazonSQS sqsClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSqsQueueHelper"/> class.
    /// </summary>
    public DefaultSqsQueueHelper(AwsConfiguration awsConfiguration, IAmazonSQS sqsClient)
    {
        this.awsConfiguration = awsConfiguration;
        this.sqsClient = sqsClient;
    }

    /// <inheritdoc/>
    public virtual async ValueTask<string> GetQueueUrlAsync(string queueName)
    {
        if (queueUrlsCache.TryGetValue(queueName, out var queueUrl))
            return queueUrl;

        // Ok, the queue is already an url
        if (queueName.StartsWith("https://sqs."))
            return queueName;

        // The queue name doesn't contain the prefix
        if (awsConfiguration.QueuePrefix?.Length > 0)
        {
            if (!queueName.StartsWith(awsConfiguration.QueuePrefix))
                queueName = $"{awsConfiguration.QueuePrefix}{queueName}";
        }

        // The queue name doesn't contain the suffix
        if (awsConfiguration.QueueSuffix?.Length > 0)
        {
            if (queueName.EndsWith(awsConfiguration.QueueSuffix))
                queueName = $"{queueName}{awsConfiguration.QueueSuffix}";
        }

        var request = new GetQueueUrlRequest
        {
            QueueName = queueName
        };

        var response = await sqsClient.GetQueueUrlAsync(request);

        queueUrlsCache.TryAdd(queueName, response.QueueUrl);

        return response.QueueUrl;
    }

    /// <inheritdoc/>
    public virtual async Task<long> GetQueueLengthAsync(string queueName, CancellationToken cancellationToken = default)
    {
        var request = new GetQueueAttributesRequest
        {
            QueueUrl = await GetQueueUrlAsync(queueName),
            AttributeNames = new List<string> { "ApproximateNumberOfMessages" }
        };

        var response = await sqsClient.GetQueueAttributesAsync(request, cancellationToken);

        if (response.Attributes.TryGetValue("ApproximateNumberOfMessages", out var messageCountStr) && int.TryParse(messageCountStr, out var messageCount))
            return messageCount;

        return 0;
    }
}
