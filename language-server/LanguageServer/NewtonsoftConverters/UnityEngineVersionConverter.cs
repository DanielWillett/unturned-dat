using Newtonsoft.Json;
using UnturnedDat.Data;

namespace UnturnedDat.LanguageServer.NewtonsoftConverters;

public sealed class UnityEngineVersionConverter : JsonConverter<UnityEngineVersion>
{
    /// <inheritdoc />
    public override UnityEngineVersion ReadJson(JsonReader reader, Type objectType, UnityEngineVersion existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return default;

        if (reader.TokenType != JsonToken.String)
            throw new JsonException($"Unexpected token {reader.TokenType} while parsing UnityEngineVersion.");

        return UnityEngineVersion.Parse((string)reader.Value!);
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, UnityEngineVersion value, JsonSerializer serializer)
    {
        if (value.Status == null)
            writer.WriteNull();
        else
            writer.WriteValue(value.ToString());
    }
}
