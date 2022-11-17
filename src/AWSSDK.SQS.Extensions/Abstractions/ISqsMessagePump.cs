namespace AWSSDK.SQS.Extensions.Abstractions;

public interface ISqsMessagePump<T> : ISqsMessagePumpBase
{
    Task PumpAsync(Func<T?, CancellationToken, Task> processMessageAsync, CancellationToken cancellationToken = default);
}
