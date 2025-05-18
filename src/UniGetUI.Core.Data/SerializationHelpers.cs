using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml;

namespace UniGetUI.Core.Data;

public static class SerializationHelpers
{
    public static Task<string> YAML_to_JSON(string YAML)
        => Task.Run(() => yaml_to_json(YAML));

    private static string yaml_to_json(string YAML)
    {
        var yamlObject = new YamlDotNet.Serialization.Deserializer().Deserialize(YAML);
        if (yamlObject is null) return "{'message': 'deserialized YAML object was null'}";
        return new YamlDotNet.Serialization.SerializerBuilder()
            .JsonCompatible()
            .Build()
            .Serialize(yamlObject);
    }

    public static Task<string> XML_to_JSON(string XML)
        => Task.Run(() => xml_to_json(XML));

    private static string xml_to_json(string XML)
    {
        var doc = new XmlDocument();
        doc.LoadXml(XML);
        if (doc.DocumentElement is null) return "{'message': 'XmlDocument.DocumentElement was null'}";
        return JsonSerializer.Serialize(_convertXmlNode(doc.DocumentElement), SerializingOptions);
    }

    private static object? _convertXmlNode(XmlNode node)
    {
        // If node has no children, return its text or attributes
        if (node.ChildNodes.Count == 1 && node.FirstChild is XmlText singleText)
        {
            return singleText.Value;
        }

        // Attributes dictionary
        var dict = new Dictionary<string, object>();
        if (node.Attributes?.Count > 0)
        {
            foreach (XmlAttribute attr in node.Attributes)
            {
                dict[$"@{attr.Name}"] = attr.Value;
            }
        }

        // Group child elements
        var children = new Dictionary<string, List<object>>();
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child is XmlElement childElement)
            {
                var value = _convertXmlNode(childElement);
                if (!children.ContainsKey(childElement.Name))
                    children[childElement.Name] = new List<object>();
                children[childElement.Name].Add(value);
            }
        }

        // Flatten repeated elements if only one group exists
        if (children.Count == 1 && dict.Count == 0)
        {
            var firstKey = children.Keys.First();
            return children[firstKey].Count == 1 ? children[firstKey][0] : children[firstKey];
        }

        // Otherwise build normal object
        foreach (var kv in children)
        {
            dict[kv.Key] = kv.Value.Count == 1 ? kv.Value[0] : kv.Value;
        }

        return dict;
    }

    public static JsonSerializerOptions SerializingOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new FlexibleBooleanConverter(), new FlexibleStringConverter(), new FlexibleListStringConverter() }
    };

    private class FlexibleBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String => bool.TryParse(reader.GetString(), out var b) ? b :
                    int.TryParse(reader.GetString(), out var i) ? i != 0 :
                    throw new JsonException("Invalid string for boolean."),
                JsonTokenType.Number => reader.TryGetInt32(out var i2) ? i2 != 0 :
                    throw new JsonException("Invalid number for boolean."),
                _ => throw new JsonException("Invalid token for boolean.")
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    private class FlexibleStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                return "";
            }
            return reader.GetString() ?? "";
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    private class FlexibleListStringConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                return [];
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                return [reader.GetString() ?? ""];
            }
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;
                    if (reader.TokenType == JsonTokenType.String)
                        list.Add(reader.GetString() ?? string.Empty);
                }
                return list;
            }
            throw new JsonException($"Unexpected token {reader.TokenType} when reading List<string>.");
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var str in value)
            {
                writer.WriteStringValue(str);
            }
            writer.WriteEndArray();
        }
    }
}


