namespace SQS.Extensions.OpenTelemetry.Metrics;

internal static class MeterTags
{
    public const string QUEUE_NAME = "sqs.extensions.queue";
    public const string MESSAGE_TYPE = "sqs.extensions.message_type";
    public const string FAILURE_TYPE = "sqs.extensions.failure_type";
}
