// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace SQS.Extensions.Abstractions;

/// <summary>
/// Contract for the message pump base
/// </summary>
public interface ISqsMessagePumpBase
{
    /// <summary>
    /// Start
    /// pumping message from SQS to the client.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task InitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop pumping message from SQS to the client.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task StopPumpAsync(CancellationToken cancellationToken = default);
}
