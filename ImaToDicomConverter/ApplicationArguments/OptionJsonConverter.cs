using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ImaToDicomConverter.ApplicationArguments;

/// <summary T="types.
/// Converts null or missing values to None, and present values to Some(value).">
/// JSON converter for LanguageExt Option
/// </summary>
internal class OptionJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(Option<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}

internal class OptionJsonConverter<T> : JsonConverter<Option<T>>
{
    public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return None;
        }

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return value == null ? None : Some(value);
    }

    public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
    {
        value.Match(
            Some: v => JsonSerializer.Serialize(writer, v, options),
            None: () => writer.WriteNullValue()
        );
    }
}

