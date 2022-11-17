using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWSSDK.SQS.Extensions.Serialization.Json.Converters;

public class TimezoneCustomConverter : JsonConverter<TimeZoneInfo>
{
    public override TimeZoneInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return TimeZoneInfo.FindSystemTimeZoneById(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, TimeZoneInfo value, JsonSerializerOptions options)
    {
        var text = value.Id;

        writer.WriteStringValue(text);
    }
}
