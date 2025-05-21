using System.Text.Json.Nodes;
using UniGetUI.PackageEngine.Classes.Serializable;

namespace UniGetUI.PackageEngine.Serializable.Tests;

public class TestSerializableUpdatesOptions
{
    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "MartiCliment.UniGetUI.Pre-Release")]
    [InlineData(false, "hey")]
    [InlineData(true, "")]
    public void ToAndFromJsonNode(bool ign, string ver)
    {
        var originalObject1 = new SerializableUpdatesOptions()
        {
            UpdatesIgnored = ign,
            IgnoredVersion = ver,
        };

        var object2 = new SerializableUpdatesOptions();
        string contents = originalObject1.AsJsonString();
        Assert.NotEmpty(contents);
        var jsonContent = JsonNode.Parse(contents);
        Assert.NotNull(jsonContent);
        object2.LoadFromJson(jsonContent);
        AreEqual(originalObject1, object2);

        var object3 = new SerializableUpdatesOptions(originalObject1.AsJsonNode());
        AreEqual(originalObject1, object3);

        var object4 = originalObject1.Copy();
        AreEqual(originalObject1, object4);
    }

    [Theory]
    [InlineData("{}", false, "")]
    [InlineData("""
                {
                  "UpdatesIgnored": true
                }
                """, true, "")]

    [InlineData("""
                {
                  "IgnoredVersion": "lol",
                  "UNKNOWN_VAL1": true,
                  "UNKNOWN_VAL2": null,
                  "UNKNOWN_VAL3": 22,
                  "UNKNOWN_VAL4": "hehe"
                }
                """, false, "lol")]
    public void FromJson(string JSON, bool ign, string ver)
    {
        Assert.NotEmpty(JSON);
        var jsonContent = JsonNode.Parse(JSON);
        Assert.NotNull(jsonContent);
        var o2 = new SerializableUpdatesOptions(jsonContent);

        Assert.Equal(ign, o2.UpdatesIgnored);
        Assert.Equal(ver, o2.IgnoredVersion);
    }

    internal static void AreEqual(SerializableUpdatesOptions o1, SerializableUpdatesOptions o2)
    {
        Assert.Equal(o1.IgnoredVersion, o2.IgnoredVersion);
        Assert.Equal(o1.UpdatesIgnored, o2.UpdatesIgnored);
    }
}
