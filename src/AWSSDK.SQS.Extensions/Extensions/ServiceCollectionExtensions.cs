// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using AWSSDK.SQS.Extensions.Abstractions;
using AWSSDK.SQS.Extensions.Configurations;
using AWSSDK.SQS.Extensions.Implementations;

using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A set of extension methods that help you to configure AWSSDK.SQS.Extensions into your dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the SQS Background consumer.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <typeparam name="TConsumerTask"></typeparam>
    public static void AddSqsConsumer<TConsumerTask>(this IServiceCollection serviceCollection)
        where TConsumerTask : class, IHostedService
    {
        serviceCollection.AddHostedService<TConsumerTask>();
    }

    /// <summary>
    /// Add AWSSDK.SQS.Extensions services to the DI with the default configuration
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The AWS Configuration.</param>
    public static void AddSqsConsumerServices(this IServiceCollection serviceCollection, Func<AwsConfiguration> configuration)
    {
        serviceCollection.AddSqsConsumerServices<DefaultSqsQueueHelper, SystemTextJsonSerializer>(configuration);
    }

    /// <summary>
    /// Add AWSSDK.SQS.Extensions services to the DI with custom serialization
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The AWS Configuration.</param>
    /// <typeparam name="TSerializer">The serializer implementation type.</typeparam>
    public static void AddSqsConsumerWithCustomSerializer<TSerializer>(this IServiceCollection serviceCollection, Func<AwsConfiguration> configuration)
        where TSerializer : class, IMessageSerializer

    {
        serviceCollection.AddSqsConsumerServices<DefaultSqsQueueHelper, IMessageSerializer>(configuration);
    }

    /// <summary>
    /// Add AWSSDK.SQS.Extensions services to the DI with custom Queue helper
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The AWS Configuration.</param>
    /// <typeparam name="TQueueHelper">The queue helper implementation type.</typeparam>
    public static void AddSqsConsumerWithCustomQueueHeper<TQueueHelper>(this IServiceCollection serviceCollection, Func<AwsConfiguration> configuration)
        where TQueueHelper : class, ISqsQueueHelper

    {
        serviceCollection.AddSqsConsumerServices<TQueueHelper, SystemTextJsonSerializer>(configuration);
    }

    /// <summary>
    /// Add AWSSDK.SQS.Extensions services to the DI
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The AWS Configuration.</param>
    /// <typeparam name="TQueueHelper">The queue helper implementation type.</typeparam>
    /// <typeparam name="TSerializer">The serializer implementation type.</typeparam>
    public static void AddSqsConsumerServices<TQueueHelper, TSerializer>(this IServiceCollection serviceCollection, Func<AwsConfiguration> configuration)
        where TSerializer : class, IMessageSerializer
        where TQueueHelper : class, ISqsQueueHelper

    {
        serviceCollection.AddSingleton(configuration());
        serviceCollection.AddSingleton<ISqsQueueHelper, TQueueHelper>();
        serviceCollection.AddSingleton<IMessageSerializer, TSerializer>();

        serviceCollection.AddSingleton<ISqsDispatcher, SqsDispatcher>();
        serviceCollection.AddSingleton<ISqsMessagePumpFactory, SqsSqsMessagePumpFactory>();
    }
}
