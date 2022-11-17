// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using AWSSDK.SQS.Extensions.Abstractions;
using AWSSDK.SQS.Extensions.Configurations;
using AWSSDK.SQS.Extensions.Implementations;

using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddSqsConsumer<TConsumerTask>(this IServiceCollection serviceCollection)
        where TConsumerTask : class, IHostedService
    {
        serviceCollection.AddHostedService<TConsumerTask>();
    }

    public static void AddSqsConsumerServices(this IServiceCollection serviceCollection, Func<AwsConfiguration> configuration)
    {
        serviceCollection.AddSqsConsumerServices<DefaultSqsQueueHelper, SystemTextJsonSerializer>(configuration);
    }

    public static void AddSqsConsumerWithCustomSerializer<TSerializer>(this IServiceCollection serviceCollection, Func<AwsConfiguration> configuration)
        where TSerializer : class, IMessageSerializer

    {
        serviceCollection.AddSqsConsumerServices<DefaultSqsQueueHelper, IMessageSerializer>(configuration);
    }

    public static void AddSqsConsumerWithCustomQueueHeper<TQueueHelper>(this IServiceCollection serviceCollection, Func<AwsConfiguration> configuration)
        where TQueueHelper : class, ISqsQueueHelper

    {
        serviceCollection.AddSqsConsumerServices<TQueueHelper, SystemTextJsonSerializer>(configuration);
    }

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
