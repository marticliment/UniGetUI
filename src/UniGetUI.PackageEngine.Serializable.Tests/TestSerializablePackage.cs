using System.Text.Json.Nodes;
using UniGetUI.PackageEngine.Classes.Serializable;

namespace UniGetUI.PackageEngine.Serializable.Tests;

public class TestSerializablePackage
{
    public static InstallOptions TestOptions = new()
    {
        SkipHashCheck = true,
        CustomParameters_Install = ["a", "b", "c"],
        CustomParameters_Update = ["b", "b", "b"],
        CustomParameters_Uninstall = ["c", "b", "a"],
        Architecture = "ia64",
        Version = "-1",
        RunAsAdministrator = true
    };

    public static SerializableUpdatesOptions TestUpdatesOpts = new()
    {
        UpdatesIgnored = true,
        IgnoredVersion = "Espresso Macchiato",
    };

    [Theory]
    [InlineData("", "", "", "", "")]
    [InlineData("UniGetUI", "MartiCliment.UniGetUI.Pre-Release", "3.2.1-beta1", "WinGet", "winget")]
    [InlineData("Mr. Trololo", "\n\n\n", "\x12", "\r", "beanz")]
    public void ToAndFromJsonNode(string id, string name, string version, string manager, string source)
    {
        var originalObject1 = new SerializablePackage()
        {
            Id = id,
            Name = name,
            Source = manager,
            Version = version
        };

        var object2 = new SerializablePackage();
        string contents = originalObject1.AsJsonString();
        Assert.NotEmpty(contents);
        var jsonContent = JsonNode.Parse(contents);
        Assert.NotNull(jsonContent);
        object2.LoadFromJson(jsonContent);
        AreEqual(originalObject1, object2);

        var object3 = new SerializablePackage(originalObject1.AsJsonNode());
        AreEqual(originalObject1, object3);

        var object4 = originalObject1.Copy();
        AreEqual(originalObject1, object4);
    }

    [Theory]
    [InlineData("{}", "", "", "", "", "", false, "")]
    [InlineData("""
                {
                  "Name": "name",
                  "Id": "true",
                  "Updates" : {
                    "IgnoredVersion": "Hey"
                  }
                }
                """, "true", "name", "", "", "", false, "Hey")]

    [InlineData("""
                 {
                    "Version": "false",
                    "Source": "lol",
                    "ManagerName": "Rodolfo Chikilicuatre",
                    "UNKNOWN_VAL1": true,
                    "UNKNOWN_VAL2": null,
                    "UNKNOWN_VAL3": 22,
                    "UNKNOWN_VAL4": "hehe",
                    "InstallationOptions" : {
                        "SkipHashCheck": true
                    }
                }
                """, "", "", "false", "Rodolfo Chikilicuatre", "lol", true, "")]

    public void FromJson(string JSON, string id, string name, string version, string manager, string source, bool skipHash, string ignoredVer)
    {
        Assert.NotEmpty(JSON);
        var jsonContent = JsonNode.Parse(JSON);
        Assert.NotNull(jsonContent);
        var o2 = new SerializablePackage(jsonContent);

        Assert.Equal(name, o2.Name);
        Assert.Equal(id, o2.Id);
        Assert.Equal(manager, o2.ManagerName);
        Assert.Equal(source, o2.Source);
        Assert.Equal(version, o2.Version);
        TestInstallOptions.AreEqual(new() { SkipHashCheck = skipHash }, o2.InstallationOptions);
        TestSerializableUpdatesOptions.AreEqual(new(){IgnoredVersion = ignoredVer}, o2.Updates);
    }

    internal static void AreEqual(SerializablePackage o1, SerializablePackage o2)
    {
        Assert.Equal(o1.Name, o2.Name);
        Assert.Equal(o1.Id, o2.Id);
        Assert.Equal(o1.Source, o2.Source);
        Assert.Equal(o1.Version, o2.Version);
        Assert.Equal(o1.ManagerName, o2.ManagerName);
        TestInstallOptions.AreEqual(o1.InstallationOptions, o2.InstallationOptions);
        TestSerializableUpdatesOptions.AreEqual(o1.Updates, o2.Updates);
    }
}
