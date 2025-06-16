using System.Text.Json.Nodes;

namespace UniGetUI.PackageEngine.Serializable.Tests;

public class TestInstallOptions
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
        var originalObject1 = new InstallOptions()
        {
            SkipHashCheck = a,
            Architecture = h,
            CustomInstallLocation = c,
            CustomParameters_Install = [d, e, f],
            CustomParameters_Update = [d, e, f, f, f, d],
            CustomParameters_Uninstall = [e, f, f],
            InstallationScope = g,
            InteractiveInstallation = b,
            PreRelease = i,
            RunAsAdministrator = j,
            SkipMinorUpdates = k,
            Version = l
        };

        Assert.Equal(a, originalObject1.DiffersFromDefault());

        var object2 = new InstallOptions();
        string contents = originalObject1.AsJsonString();
        Assert.NotEmpty(contents);
        var jsonContent = JsonNode.Parse(contents);
        Assert.NotNull(jsonContent);
        object2.LoadFromJson(jsonContent);
        AreEqual(originalObject1, object2);

        var object3 = new InstallOptions(originalObject1.AsJsonNode());
        AreEqual(originalObject1, object3);

        var object4 = originalObject1.Copy();
        AreEqual(originalObject1, object4);
    }

    [Theory]
    [InlineData("{}", false, false, "", "", "", "", "", "", false, false, false, "", false)]
    [InlineData("""
                {
                  "SkipHashCheck": true,
                  "InteractiveInstallation": true,
                  "RunAsAdministrator": false,
                  "Architecture": "lol",
                  "InstallationScope": "",
                  "CustomParameters": [
                    "a"
                  ]
                }
                """, true, true, "", "a", "", "", "", "lol", false, false, false, "", true)]

    [InlineData("""
                {
                  "PreRelease": false,
                  "CustomInstallLocation": "",
                  "Version": "heyheyhey",
                  "SkipMinorUpdates": true,
                  "UNKNOWN_VAL1": true,
                  "UNKNOWN_VAL2": null,
                  "UNKNOWN_VAL3": 22,
                  "UNKNOWN_VAL4": "hehe"
                }
                """, false, false, "", "", "", "",
        "", "", false, false, true, "heyheyhey", true)]
    public void FromJson(string JSON, bool hash, bool inter, string installLoc, string arg1, string arg2, string arg3, string scope, string arch, bool pre, bool admin, bool skipMin, string ver, bool mod)
    {
        Assert.NotEmpty(JSON);
        var jsonContent = JsonNode.Parse(JSON);
        Assert.NotNull(jsonContent);
        var o2 = new InstallOptions(jsonContent);

        var list = new List<string>() { arg1, arg2, arg3 }.Where(x => x.Any());

        Assert.Equal(mod, o2.OverridesNextLevelOpts);
        Assert.Equal(mod, o2.DiffersFromDefault());
        Assert.Equal(hash, o2.SkipHashCheck);
        Assert.Equal(arch, o2.Architecture);
        Assert.Equal(installLoc, o2.CustomInstallLocation);
        Assert.Equal(list.Where(x => x.Any()).ToList(), o2.CustomParameters_Install.Where(x => x.Any()).ToList());
        Assert.Equal(list.Where(x => x.Any()).ToList(), o2.CustomParameters_Update.Where(x => x.Any()).ToList());
        Assert.Equal(list.Where(x => x.Any()).ToList(), o2.CustomParameters_Uninstall.Where(x => x.Any()).ToList());
        Assert.Equal(scope, o2.InstallationScope);
        Assert.Equal(inter, o2.InteractiveInstallation);
        Assert.Equal(pre, o2.PreRelease);
        Assert.Equal(admin, o2.RunAsAdministrator);
        Assert.Equal(skipMin, o2.SkipMinorUpdates);
        Assert.Equal(ver, o2.Version);
    }

    internal static void AreEqual(InstallOptions o1, InstallOptions o2)
    {
        Assert.Equal(o1.SkipHashCheck, o2.SkipHashCheck);
        Assert.Equal(o1.Architecture, o2.Architecture);
        Assert.Equal(o1.CustomInstallLocation, o2.CustomInstallLocation);
        Assert.Equal(o1.CustomParameters_Install.Where(x => x.Any()).ToList(), o2.CustomParameters_Install.Where(x => x.Any()).ToList());
        Assert.Equal(o1.CustomParameters_Uninstall.Where(x => x.Any()).ToList(), o2.CustomParameters_Uninstall.Where(x => x.Any()).ToList());
        Assert.Equal(o1.CustomParameters_Update.Where(x => x.Any()).ToList(), o2.CustomParameters_Update.Where(x => x.Any()).ToList());
        Assert.Equal(o1.InstallationScope, o2.InstallationScope);
        Assert.Equal(o1.InteractiveInstallation, o2.InteractiveInstallation);
        Assert.Equal(o1.PreRelease, o2.PreRelease);
        Assert.Equal(o1.RunAsAdministrator, o2.RunAsAdministrator);
        Assert.Equal(o1.SkipMinorUpdates, o2.SkipMinorUpdates);
        Assert.Equal(o1.Version, o2.Version);
    }
}
