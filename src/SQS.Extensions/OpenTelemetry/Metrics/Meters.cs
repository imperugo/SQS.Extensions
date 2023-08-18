using System.Diagnostics.Metrics;

namespace SQS.Extensions.OpenTelemetry.Metrics;

internal static class Meters
{
    internal static readonly Meter SqsMeter = new Meter("SQS.Extensions", "2.0.0");

    internal static readonly Counter<long> TotalProcessedSuccessfully = SqsMeter.CreateCounter<long>("sqs.extensions.messaging.successes", description: "Total number of messages processed successfully.");

    internal static readonly Counter<long> TotalFetched = SqsMeter.CreateCounter<long>("sqs.extensions.messaging.fetches", description: "Total number of messages fetched from the queue.");

    internal static readonly Counter<long> TotalFailures = SqsMeter.CreateCounter<long>("sqs.extensions.messaging.failures", description: "Total number of messages processed unsuccessfully.");
}
