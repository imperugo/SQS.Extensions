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
    /// <param name="originalMessage">The SQS Message with it all attributes.</param>
    public MessageContext(Message originalMessage)
    {
        Message = originalMessage;
    }

    /// <summary>
    /// Gets the identifier of the message.
    /// </summary>
    /// <value>The message identifier.</value>
    public Message Message { get; }

    /// <summary>
    /// Gets or sets the retry count for the message.
    /// </summary>
    /// <value>The retry count for the message.</value>
    public int? RetryCount { get; internal set; }

    /// <summary>
    /// Get the value of a message attribute.
    /// </summary>
    /// <param name="key">The key of the dictionary.</param>
    public string? GetAttributeAsString(string key)
    {
        if (Message.MessageAttributes.TryGetValue(key, out var value))
            return value.StringValue;

        return null;
    }

    /// <summary>
    /// Get the value of a message attribute as an integer.
    /// </summary>
    /// <param name="key">The key of the dictionary.</param>
    /// <param name="defaultValue">The default value to return</param>
    public int? GetAttributeAsInt(string key, int? defaultValue = null)
    {
        if (Message.MessageAttributes.TryGetValue(key, out var value))
            return int.Parse(value.StringValue);

        return defaultValue;
    }
}
