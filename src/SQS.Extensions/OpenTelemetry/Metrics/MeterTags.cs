namespace SQS.Extensions.OpenTelemetry.Metrics;

internal static class MeterTags
{
    public const string QueueUrl = "sqs.extensions.queue";
    public const string MessageType = "sqs.extensions.message_type";
    public const string FailureType = "sqs.extensions.failure_type";
}
