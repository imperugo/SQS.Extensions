namespace AWSSDK.SQS.Extensions.Abstractions;

public interface ISqsMessagePumpBase
{
    Task StopPumpAsync(CancellationToken cancellationToken = default);
}
