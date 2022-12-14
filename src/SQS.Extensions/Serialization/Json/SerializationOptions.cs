using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using SQS.Extensions.Serialization.Json.Converters;

namespace SQS.Extensions.Serialization.Json;

internal static class SerializationOptions
{
    public static JsonSerializerOptions Default
    {
        get
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                AllowTrailingCommas = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            options.Converters.Add(new CultureCustomConverter());
            options.Converters.Add(new TimezoneCustomConverter());
            options.Converters.Add(new TimeSpanConverter());

#if NET6_0_OR_GREATER
            options.Converters.Add(new DateOnlyConverter());
            options.Converters.Add(new DateOnlyNullableConverter());
#endif
            options.Converters.Add(new JsonStringEnumConverter(null, false));

            return options;
        }
    }
}
