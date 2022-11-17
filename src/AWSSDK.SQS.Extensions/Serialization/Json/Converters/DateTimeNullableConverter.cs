using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWSSDK.SQS.Extensions.Serialization.Json.Converters;

public class DateTimeNullableConverter : JsonConverter<DateTime?>
{
    private const string DefaultDateTimeFormat = "yyyy-MM-dd";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var name = reader.GetString();

        if (name?.Length > 0)
            return DateTime.ParseExact(name, DefaultDateTimeFormat, CultureInfo.InvariantCulture);

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
            return;

        var date = value.Value.ToString(DefaultDateTimeFormat, CultureInfo.InvariantCulture);
        writer.WriteStringValue(date);
    }
}
