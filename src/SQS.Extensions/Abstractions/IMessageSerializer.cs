namespace SQS.Extensions.Abstractions;

/// <summary>
/// The contract used for serializing / deserializing object into SQS messages
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Serializes the specified item.
    /// </summary>
    /// <param name="itemToSerialized">The item.</param>
    /// <returns>Return the serialized object</returns>
    string Serialize<T>(T itemToSerialized);

    /// <summary>
    /// Deserializes the specified bytes.
    /// </summary>
    /// <typeparam name="T">The type of the expected object.</typeparam>
    /// <param name="serializedObject">The serialized object.</param>
    /// <returns>
    /// The instance of the specified Item
    /// </returns>
    T? Deserialize<T>(string serializedObject);
}
