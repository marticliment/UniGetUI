using System.Text.Json.Nodes;
using UniGetUI.PackageEngine.Classes.Serializable;

namespace UniGetUI.PackageEngine.Serializable.Tests;

public class TestSerializableBundle
{
    public static SerializablePackage TestPackage1 = new()
    {
        Name = "Package",
        Id = "Identifier1",
        ManagerName = "Scoop",
        Source = "",
        Version = "3.0",
        InstallationOptions = new()
        {
            SkipHashCheck = true,
            Architecture = "4+1€",
            CustomParameters_Install = ["--hello-world", "--another-param", "-help"],
            CustomParameters_Update = ["--update", "--another-param", "-help"],
            CustomParameters_Uninstall = ["--uninstall", "--another-param", "-help"]
        },
        Updates = new()
        {
            IgnoredVersion = "12",
            UpdatesIgnored = false
        }
    };

    public static SerializablePackage TestPackage2 = new()
    {
        Name = "AnotherPackage",
        Id = "Identifier2",
        ManagerName = "WinGet",
        Source = "msstore",
        Version = "5.0",
    };

    public static SerializableIncompatiblePackage TestIncompatiblePackage = new()
    {
        Name = "AnotherPackage3",
        Id = "Identifier4",
        Source = "msstore",
        Version = "5.0",
    };


    [Fact]
    public void ToAndFromJsonNode()
    {
        var originalObject1 = new SerializableBundle()
        {
            export_version = 5,
            packages = [TestPackage1, TestPackage2],
            incompatible_packages_info = "I'm trying to reach you regarding your car's extended warranty",
            incompatible_packages = [TestIncompatiblePackage]
        };

        var object2 = new SerializableBundle();
        string contents = originalObject1.AsJsonString();
        Assert.NotEmpty(contents);
        var jsonContent = JsonNode.Parse(contents);
        Assert.NotNull(jsonContent);
        object2.LoadFromJson(jsonContent);
        AreEqual(originalObject1, object2);

        var object3 = new SerializableBundle(originalObject1.AsJsonNode());
        AreEqual(originalObject1, object3);

        var object4 = originalObject1.Copy();
        AreEqual(originalObject1, object4);
    }

    [Theory]
    [InlineData("{}", "", "", "")]
    [InlineData("""
                {
                  "export_version": 2.1,
                  "packages": [
                    {
                      "Id": "Hello"
                    },
                    {
                      "Id": "World"
                    }
                  ],
                  "incompatible_packages_info": "hey",
                  "incompatible_packages": [
                    {
                      "Id": "3"
                    }
                  ]
                }
                """, "Hello", "World", "3")]
    public void FromJson(string JSON, string id1, string id2, string id3)
    {
        Assert.NotEmpty(JSON);
        var jsonContent = JsonNode.Parse(JSON);
        Assert.NotNull(jsonContent);
        var o2 = new SerializableBundle(jsonContent);

        if (id1 == "")
        {
            Assert.Equal(0, o2.export_version);
            Assert.Equal(SerializableBundle.IncompatMessage, o2.incompatible_packages_info);
            Assert.Empty(o2.packages);
            Assert.Empty(o2.incompatible_packages);
        }
        else
        {
            Assert.Equal(2.1, o2.export_version);
            Assert.Equal("hey", o2.incompatible_packages_info);
            Assert.Equal(id1, o2.packages[0].Id);
            Assert.Equal(id2, o2.packages[1].Id);
            Assert.Equal(id3, o2.incompatible_packages[0].Id);
        }
    }

    internal static void AreEqual(SerializableBundle o1, SerializableBundle o2)
    {
        Assert.Equal(o1.export_version, o2.export_version);
        Assert.Equal(o1.incompatible_packages_info, o2.incompatible_packages_info);

        Assert.Equal(o1.packages.Count, o2.packages.Count);
        for (int i = 0; i < o1.packages.Count; i++)
            TestSerializablePackage.AreEqual(o1.packages[i], o2.packages[i]);

        Assert.Equal(o1.incompatible_packages.Count, o2.incompatible_packages.Count);
        for (int i = 0; i < o1.incompatible_packages.Count; i++)
            TestSerializableIncompatiblePackage.AreEqual(o1.incompatible_packages[i], o2.incompatible_packages[i]);
    }
}
