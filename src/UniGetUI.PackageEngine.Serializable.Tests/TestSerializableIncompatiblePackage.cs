using System.Text.Json.Nodes;
using UniGetUI.PackageEngine.Classes.Serializable;

namespace UniGetUI.PackageEngine.Serializable.Tests;

public class TestSerializableIncompatiblePackage
{
    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("UniGetUI", "MartiCliment.UniGetUI.Pre-Release", "3.2.1-beta1", "WinGet")]
    [InlineData("Mr. Trololo", "\n\n\n", "\x12", "\r")]
    public void ToAndFromJsonNode(string id, string name, string version, string manager)
    {
        var originalObject1 = new SerializableIncompatiblePackage()
        {
            Id = id,
            Name = name,
            Source = manager,
            Version = version
        };

        var object2 = new SerializableIncompatiblePackage();
        string contents = originalObject1.AsJsonString();
        Assert.NotEmpty(contents);
        var jsonContent = JsonNode.Parse(contents);
        Assert.NotNull(jsonContent);
        object2.LoadFromJson(jsonContent);
        AreEqual(originalObject1, object2);

        var object3 = new SerializableIncompatiblePackage(originalObject1.AsJsonNode());
        AreEqual(originalObject1, object3);

        var object4 = originalObject1.Copy();
        AreEqual(originalObject1, object4);
    }

    [Theory]
    [InlineData("{}", "", "", "", "")]
    [InlineData("""
                {
                  "Name": "name",
                  "Id": "true"
                }
                """, "true", "name", "", "")]

    [InlineData("""
                {
                  "Version": "false",
                  "Source": "lol",
                  "UNKNOWN_VAL1": true,
                  "UNKNOWN_VAL2": null,
                  "UNKNOWN_VAL3": 22,
                  "UNKNOWN_VAL4": "hehe"
                }
                """, "", "", "false", "lol")]
    public void FromJson(string JSON, string id, string name, string version, string manager)
    {
        Assert.NotEmpty(JSON);
        var jsonContent = JsonNode.Parse(JSON);
        Assert.NotNull(jsonContent);
        var o2 = new SerializableIncompatiblePackage(jsonContent);

        Assert.Equal(name, o2.Name);
        Assert.Equal(id, o2.Id);
        Assert.Equal(manager, o2.Source);
        Assert.Equal(version, o2.Version);

    }

    internal static void AreEqual(SerializableIncompatiblePackage o1, SerializableIncompatiblePackage o2)
    {
        Assert.Equal(o1.Name, o2.Name);
        Assert.Equal(o1.Id, o2.Id);
        Assert.Equal(o1.Source, o2.Source);
        Assert.Equal(o1.Version, o2.Version);
    }
}
