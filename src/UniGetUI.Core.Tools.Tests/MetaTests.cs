using System.Text.RegularExpressions;

namespace UniGetUI.Core.Tools.Tests;

public class MetaTests
{
    [Fact]
    public void TestJsonSerializationOptions()
    {
        // This test ensures that any json operation has the proper serialization options set (required for TRIM support)
        var solutionDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\.."));
        var csFiles = Directory.GetFiles(solutionDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(@"bin\") && !file.Contains(@"obj\") && !file.EndsWith(".g.cs") && !file.EndsWith("Tests.cs"));

        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            var jsonSerCount = lines.Count(x => x.Contains("JsonSerializer.Serialize"));
            var jsonDeserCount = lines.Count(x => x.Contains("JsonSerializer.Deserialize"));
            var serialOptionsCount1 = lines.Count(x => x.Contains("SerializationHelpers.DefaultOptions"));
            var serialOptionsCount2 = lines.Count(x => x.Contains("SerializationHelpers.ImportBundleOptions"));
            var serialOptionsCount3 = lines.Count(x => x.Contains("SerializationOptions"));
            Assert.True((jsonSerCount + jsonDeserCount) <= serialOptionsCount1 + serialOptionsCount2 + serialOptionsCount3,
                $"Failing on {file}. The specified file does not serialize and/or deserialize JSON with" +
                $" the proper SerializationHelpers.DefaultOptions set");
        }
    }

    [Fact]
    public void TestHttpClientInstantiation()
    {
        // This test ensures that any instantiation of HttpClient contains at least one empty line after it
        var solutionDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\.."));
        var csFiles = Directory.GetFiles(solutionDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(@"bin\") && !file.Contains(@"obj\") && !file.EndsWith(".g.cs")
                           && !file.EndsWith("Tests.cs") && !file.EndsWith("LanguageEngine.cs"));

        foreach (var file in csFiles)
        {
            var fileContent = File.ReadAllText(file);
            var match = Regex.Match(fileContent, "new HttpClient ?\\(\\)");
            var match2 = Regex.Match(fileContent, "HttpClient [a-zA-Z0-9_]+ ?= ?new ?\\(\\)");
            Assert.False(match.Success || match2.Success,
                $"File {file} has an incorrect instantiation of HttpClient, that does not pass the " +
                $"compulsory CoreTools.GenericHttpClientParameters param");
        }
    }


}
