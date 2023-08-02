// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace SQS.Extensions.Configurations;

/// <summary>
/// The AWS Configuration
/// </summary>
public sealed record AwsConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsConfiguration"/> class.
    /// </summary>
    public AwsConfiguration()
    {
    }

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
    public string? QueueSuffix { get; set; }
}
