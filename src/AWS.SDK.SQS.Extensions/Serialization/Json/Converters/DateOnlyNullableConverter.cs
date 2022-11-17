#if NET6_0 || NET7_0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.SDK.SQS.Extensions.Serialization.Json.Converters;

internal class DateOnlyNullableConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valueAsString = reader.GetString();

        if (valueAsString?.Length > 0)
            return DateOnly.ParseExact(reader.GetString()!, "yyyy-MM-dd");

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value == null)
            return;

        writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
    }
}

#endif
