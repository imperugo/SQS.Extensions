using AWSSDK.SQS.Extensions.Abstractions;
using AWSSDK.SQS.Extensions.Configurations;

namespace AWSSDK.SQS.Extensions.Implementations;

/// <summary>
/// Default implementation of <see cref="ISqsQueueHelper"/>
/// </summary>
internal sealed class DefaultSqsQueueHelper : ISqsQueueHelper
{
    private readonly AwsConfiguration awsConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSqsQueueHelper"/> class.
    /// </summary>
    public DefaultSqsQueueHelper(AwsConfiguration awsConfiguration)
    {
        this.awsConfiguration = awsConfiguration;
    }

    /// <inheritdoc/>
    public string GetQueueUrl(string queueName)
    {
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

        return $"https://sqs.{awsConfiguration.Region}.amazonaws.com/{awsConfiguration.AccountId}/{queueName}";
    }
}
