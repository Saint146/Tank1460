using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Tank1460.Converters;

internal class JsonStringEnumArrayConverter<T> : JsonConverter<T[]> where T : Enum
{
    public override T[] ReadJson(JsonReader reader, Type objectType, T[] existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var items = new List<T>();
        if (reader.TokenType == JsonToken.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;

                var item = serializer.Deserialize<T>(reader);
                if (item is null)
                    continue;

                items.Add(item);
            }
        }

        return items.ToArray();
    }


    public override void WriteJson(JsonWriter writer, T[] value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}