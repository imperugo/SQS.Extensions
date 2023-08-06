using SQS.Extensions.Abstractions;

using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;

using SQS.Extensions.Configurations;
using SQS.Extensions.Sample;
using SQS.Extensions.Sample.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultAWSOptions(new AWSOptions { Region = RegionEndpoint.EUCentral1 });
builder.Services.AddAWSService<IAmazonSQS>();

// Configuration
builder.Services.AddSqsConsumerServices(() => new AwsConfiguration
{
    QueuePrefix = "develop-"
});

// Consumer registrations
builder.Services.AddSqsConsumer<Consumer1>();
// builder.Services.AddSqsConsumer<Consumer2>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapPost("/SendMessageToQueue", async (
    MySqsMessage request,
    ISqsDispatcher sqsDispatcher,
    CancellationToken cancellationToken) =>
{
    // Do your stuff
    await sqsDispatcher.QueueAsync(request, Constants.QUEUE_NAME_1, 0, cancellationToken);
    // await sqsDispatcher.QueueAsync(request, Constants.QUEUE_NAME_2, cancellationToken);

    return Results.NoContent();
});

app.Run();
