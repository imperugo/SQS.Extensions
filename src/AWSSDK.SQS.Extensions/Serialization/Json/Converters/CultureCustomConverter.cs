using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWSSDK.SQS.Extensions.Serialization.Json.Converters;

internal class CultureCustomConverter : JsonConverter<CultureInfo>
{
    public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new CultureInfo(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options)
    {
        var text = value.Name;

        writer.WriteStringValue(text);
    }
}
