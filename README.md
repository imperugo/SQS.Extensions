# AWS.SDK.SQS.Extensions

[![Nuget](https://img.shields.io/nuget/v/AWS.SDK.SQS.Extensions?style=flat-square)](https://www.nuget.org/packages/AWS.SDK.SQS.Extensions/)
[![Nuget](https://img.shields.io/nuget/vpre/AWS.SDK.SQS.Extensions?style=flat-square)](https://www.nuget.org/packages/AWS.SDK.SQS.Extensions/)
[![GitHub](https://img.shields.io/github/license/imperugo/AWS.SDK.SQS.Extensions?style=flat-square)](https://github.com/imperugo/AWS.SDK.SQS.Extensions/blob/main/LICENSE)

AWS.SDK.SQS.Extensions is a .NET library with the idea to helps developer using AWS Sqs.

Behind the scenes it covers a set of repetitive tasks, handle complexity and highly customizable.

## Quickstart

### Installation

Add the NuGet Package to your project:

```bash
dotnet add package AWS.SDK.SQS.Extensions
```

### Configuration

Configuration is pretty simple if you don't want any particular customization. 

```c#
// This is needed by AWS library
builder.Services.AddDefaultAWSOptions(new AWSOptions { Region = RegionEndpoint.EUCentral1 });
builder.Services.AddAWSService<IAmazonSQS>();

// Dependency Injection
// Configuration
builder.Services.AddSqsConsumerServices(
    () => new AwsConfiguration(region: "eu-central-1", accountId: "775704350706")
{
    QueuePrefix = "develop-"
});

// Consumer registrations
// this is needed only in you have to dequeue from SQS
// in case of send only is not needed
builder.Services.AddSqsConsumer<MySqsConsumer>();
```

### Sending message to queue

```c#
app.MapPost("/SendMessageToQueue", async (
    MySqsMessage request,
    ISqsDispatcher sqsDispatcher,
    CancellationToken cancellationToken) =>
{
    // Do your stuff
    await sqsDispatcher.QueueAsync(request, Constants.QUEUE_NAME_1, cancellationToken);

    return Results.NoContent();
});
```

### Pooling message from queue

The library allows you to receive messages using a background task. Everytime a new message came into the queue, you function will be invocated.

> You could register multiple consumers with different queues

```c#
internal sealed class MySqsConsumer : SqsHostedService<MySqsMessage>
{
    public MySqsConsumer(
        ILogger<MySqsConsumer> logger,
        ISqsMessagePumpFactory messagePumpFactory)
            : base(logger, messagePumpFactory)
    {
    }

    protected override Func<MySqsMessage?, CancellationToken, Task> ProcessMessageFunc => ConsumeMessageAsync;

    protected override MessagePumpConfiguration MessagePumpConfiguration =>
        new (Constants.QUEUE_NAME_1)
        {
            // waiting time between calls to SQS.
            BatchDelay = TimeSpan.FromSeconds(10),

            // the max number of concurrent operations
            MaxConcurrentOperation = 10,

            // if true every time the app start cleans the queue
            // helpful for testing
            PurgeOnStartup = true
        };

    private Task ConsumeMessageAsync(MySqsMessage? message, CancellationToken cancellationToken)
    {
        // Do your staff here

        return Task.CompletedTask;
    }
}
```

### Customization

#### Serialization

The library allows you to choice your favorite serialization library. If you don't specify anything, the recente System.Text.Json will be used.
If you prefer to use something different you can create your own serializer in this way

```c#
public sealed class MySuperSerializer : IMessageSerializer
{
    public string Serialize<T>(T itemToSerialized)
    {
    }

    public T? Deserialize<T>(string serializedObject)
    {
        
    }
}
```

Then register it on DI

```csharp
builder.Services.AddSqsConsumerWithCustomSerializer<MySuperSerializer>(() => new AwsConfiguration(region: "eu-central-1", accountId: "775704350706")
{
    QueuePrefix = "develop-"
});
```

#### Queue name customization

Out of the box the library offers you the opportunity to add automatically a prefix and/or suffix to the queue name.
Sometimes this could be helpful for temporary queue or for different environment like development and production (would be better to use different account for security reason)

When you add the library to your DI you can specify these parameters

```csharp
// Dependency Injection
builder.Services.AddSqsConsumerServices(
    () => new AwsConfiguration(region: "eu-central-1", accountId: "775704350706")
{
    QueuePrefix = "develop-",
    QueueSuffix = "-temp"
});
```

If you don't like this way or you have a super strange queue name algorithm you can add your custom logic in this way:

```csharp
internal sealed class MySqsQueueHelper : ISqsQueueHelper
{
    private readonly AwsConfiguration awsConfiguration;

    public DefaultSqsQueueHelper(AwsConfiguration awsConfiguration)
    {
        this.awsConfiguration = awsConfiguration;
    }

    public string GetQueueName(string queueName)
    {
        // do whatever you have to do
        return $"https://sqs.{awsConfiguration.Region}.amazonaws.com/{awsConfiguration.AccountId}/{MyCalculatedQueueName}";
    }
}
```

Then register it on DI

```csharp
builder.Services.AddSqsConsumerWithCustomQueueHeper<MySqsQueueHelper>(
    () => new AwsConfiguration(region: "eu-central-1", accountId: "775704350706"));
```

## Sample

Take a look [here](https://github.com/imperugo/AWS.SDK.SQS.Extensions/blob/main/sample/AWS.SDK.SQS.Extensions.Sample****)

## License

AWS.SDK.SQS.Extensions [MIT](https://github.com/imperugo/AWS.SDK.SQS.Extensions/blob/main/LICENSE) licensed.

### Contributing

Thanks to all the people who already contributed!

<a href="https://github.com/imperugo/AWS.SDK.SQS.Extensions/graphs/contributors">
  <img src="https://contributors-img.web.app/image?repo=imperugo/AWS.SDK.SQS.Extensions" />
</a>
