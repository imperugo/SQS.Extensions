namespace AWSSDK.SQS.Extensions.Abstractions;

public interface IMessageSerializer
{
    string Serialize<T>(T itemToSerialized);
    T? Deserialize<T>(string serializedObject);
}
