namespace SQS.Extensions.Sample.Consumers;

public sealed class MySqsMessage
{
    public string? Title { get; set; }
    public DateTime CreatedOn { get; set; }
}