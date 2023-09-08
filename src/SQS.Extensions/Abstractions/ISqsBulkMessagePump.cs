// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace SQS.Extensions.Abstractions;

/// <summary>
/// Contract for the bulk message pump
/// </summary>
public interface ISqsBulkMessagePump<TMessage> : ISqsMessagePumpBase where TMessage : notnull
{
    /// <summary>
    /// Start pump messages from SQS.
    /// </summary>
    /// <param name="processMessageAsync">The function to call when a message has been caught.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PumpAsync(Func<Dictionary<TMessage, MessageContext>, CancellationToken, Task> processMessageAsync, CancellationToken cancellationToken = default);
}
