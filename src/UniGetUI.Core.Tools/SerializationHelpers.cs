using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        return JsonSerializer.Serialize(_convertXmlNode(doc.DocumentElement), SerializationHelpers.DefaultOptions);
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

    public static JsonSerializerOptions DefaultOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        AllowTrailingCommas = true,
        WriteIndented = true,
    };
}

public static class JsonNodeExtensions
{
    /// <summary>
    /// Safely gets a child node by key, returning null if the key does not exist.
    /// </summary>
    public static T GetVal<T>(this JsonNode node)
    {
        if (typeof(T) == typeof(double) && node.GetValueKind() is JsonValueKind.String)
            return (T)(object)double.Parse(node.GetValue<string>(), CultureInfo.InvariantCulture);

        if (typeof(T) == typeof(int) && node.GetValueKind() is JsonValueKind.String)
            return (T)(object)int.Parse(node.GetValue<string>());

        if (typeof(T) == typeof(bool) && node.GetValueKind() is JsonValueKind.String)
            return (T)(object)bool.Parse(node.GetValue<string>());

        if (typeof(T) == typeof(string) && node.GetValueKind() is JsonValueKind.Object)
        {
            return (T)(object)"";
        }

        return node.GetValue<T>();
    }

    /// <summary>
    /// The same as JsonNode.AsArray, but can convert objects whose keys are integers to arrays
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static JsonArray AsArray2(this JsonNode node)
    {
        if (node is JsonValue val)
        {
            JsonArray result = new();
            result.Add(val.DeepClone());
            return result;
        }
        else if (node is JsonObject obj && !obj.Any())
        {
            return new JsonArray();
        }

        return node.AsArray();
    }
}
