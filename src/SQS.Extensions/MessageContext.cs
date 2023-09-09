// Copyright (c) Ugo Lattanzi.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Amazon.SQS.Model;

namespace SQS.Extensions;

/// <summary>
/// Represents the context of a message.
/// </summary>
public sealed class MessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContext"/> class.
    /// </summary>
    /// <param name="messageId">The SQS message identifier.</param>
    /// <param name="messageAttributes">The message attributes.</param>
    public MessageContext(string messageId, Dictionary<string, string> messageAttributes)
    {
        MessageId = messageId;
        MessageAttributes = messageAttributes;
    }

    /// <summary>
    /// Gets the identifier of the message.
    /// </summary>
    /// <value>The message identifier.</value>
    public string MessageId { get; }

    /// <summary>
    /// Gets or sets the retry count for the message.
    /// </summary>
    /// <value>The retry count for the message.</value>
    public int? RetryCount { get; internal set; }

    /// <summary>
    /// Gets the message attributes as key-value pairs.
    /// </summary>
    /// <value>A dictionary containing the message attributes.</value>
    public Dictionary<string, string> MessageAttributes { get; }

    /// <summary>
    /// Get the value of a message attribute.
    /// </summary>
    /// <param name="key">The key of the dictionary.</param>
    public string? GetAttributeAsString(string key)
    {
        if (MessageAttributes.TryGetValue(key, out var value))
            return value;

        return null;
    }

    /// <summary>
    /// Get the value of a message attribute as an integer.
    /// </summary>
    /// <param name="key">The key of the dictionary.</param>
    public int? GetAttributeAsInt(string key)
    {
        if (MessageAttributes.TryGetValue(key, out var value))
            return int.Parse(value);

        return null;
    }
}
