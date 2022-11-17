// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace AWSSDK.SQS.Extensions.Configurations;

public sealed record class AwsConfiguration
{
    /// <summary>
    ///     The AWS Region
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    ///     The AWS Account Identifier
    /// </summary>
    public required string AccountId { get; init; }

    /// <summary>
    ///     The prefix to use for the queue name
    /// </summary>
    /// <example>
    ///     develop-my_queue_name
    /// </example>
    public string? QueuePrefix { get; set; }

    /// <summary>
    ///     The suffix to use for the queue name
    /// </summary>
    /// <example>
    ///     my_queue_name-develop
    /// </example>
    public string? QueueSuffix { get; init; }
}
