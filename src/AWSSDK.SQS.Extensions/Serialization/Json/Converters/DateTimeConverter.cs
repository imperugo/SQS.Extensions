using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWSSDK.SQS.Extensions.Serialization.Json.Converters;

public class DateTimeConverter : JsonConverter<DateTime>
{
    private const string DefaultDateTimeFormat = "yyyy-MM-dd";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.ParseExact(reader.GetString()!, DefaultDateTimeFormat, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var text = value.ToString(DefaultDateTimeFormat, CultureInfo.InvariantCulture);

        writer.WriteStringValue(text);
    }
}
