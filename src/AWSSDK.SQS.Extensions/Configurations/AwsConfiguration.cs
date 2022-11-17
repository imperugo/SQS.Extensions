// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace AWSSDK.SQS.Extensions.Configurations;

/// <summary>
/// The AWS Configuration
/// </summary>
public sealed record class AwsConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsConfiguration"/> class.
    /// </summary>
    /// <param name="region">The AWS Region.</param>
    /// <param name="accountId">The AWS Account Identifier</param>
    public AwsConfiguration(string region, string accountId)
    {
        Region = region;
        AccountId = accountId;
    }

    /// <summary>
    ///     The AWS Region
    /// </summary>
    public string Region { get; }

    /// <summary>
    ///     The AWS Account Identifier
    /// </summary>
    public string AccountId { get;  }

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
