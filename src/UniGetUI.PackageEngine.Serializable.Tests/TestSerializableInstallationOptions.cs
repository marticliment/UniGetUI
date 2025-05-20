using System.Text.Json.Nodes;

namespace UniGetUI.PackageEngine.Serializable.Tests;

public class TestSerializableInstallationOptions
{
    [Theory]
    [InlineData(false, false, "", "", "", "", "", "", false, false, false, "")]
    [InlineData(true, true, "testval", "testval", "testval", "testval", "testval", "testval", true, true, true,
        "testval")]
    [InlineData(true, false, "true", "helloWorld", "testval", "heheheheeheh", "--parse\n-int", "12", false, true, false,
        "4.4.0-beta2")]
    public void ToAndFromJsonNode(bool a, bool b, string c, string d, string e, string f, string g, string h, bool i,
        bool j, bool k, string l)
    {
        var originalObject1 = new SerializableInstallationOptions()
        {
            SkipHashCheck = a,
            Architecture = h,
            CustomInstallLocation = c,
            CustomParameters = [d, e, f],
            InstallationScope = g,
            InteractiveInstallation = b,
            PreRelease = i,
            RunAsAdministrator = j,
            SkipMinorUpdates = k,
            Version = l
        };

        var object2 = new SerializableInstallationOptions();
        string contents = originalObject1.AsJsonString();
        Assert.NotEmpty(contents);
        var jsonContent = JsonNode.Parse(contents);
        Assert.NotNull(jsonContent);
        object2.LoadFromJson(jsonContent);

        AreEqual(originalObject1, object2);

        var object3 = new SerializableInstallationOptions(originalObject1.AsJsonNode());

        AreEqual(originalObject1, object3);
    }

    private static void AreEqual(SerializableInstallationOptions o1, SerializableInstallationOptions o2)
    {
        Assert.Equal(o1.SkipHashCheck, o2.SkipHashCheck);
        Assert.Equal(o1.Architecture, o2.Architecture);
        Assert.Equal(o1.CustomInstallLocation, o2.CustomInstallLocation);
        Assert.Equal(o1.CustomParameters, o2.CustomParameters);
        Assert.Equal(o1.InstallationScope, o2.InstallationScope);
        Assert.Equal(o1.InteractiveInstallation, o2.InteractiveInstallation);
        Assert.Equal(o1.PreRelease, o2.PreRelease);
        Assert.Equal(o1.RunAsAdministrator, o2.RunAsAdministrator);
        Assert.Equal(o1.SkipMinorUpdates, o2.SkipMinorUpdates);
        Assert.Equal(o1.Version, o2.Version);
    }
}
