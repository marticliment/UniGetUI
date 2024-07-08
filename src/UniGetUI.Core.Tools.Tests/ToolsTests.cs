using UniGetUI.Core.Language;

namespace UniGetUI.Core.Tools.Tests
{
    public class ToolsTests
    {
        [Theory]
        [InlineData("NonExistentString", false)]
        [InlineData(" ", false)]
        [InlineData("", false)]
        [InlineData("@@", false)]
        [InlineData("0 packages found", true)]
        [InlineData("Add packages or open an existing bundle", true)]
        public void TranslateFunctionTester(string textEntry, bool TranslationExists)
        {
            LanguageEngine langEngine = new("fr");
            CoreTools.ReloadLanguageEngineInstance("fr");

            Assert.Equal(CoreTools.Translate(textEntry), langEngine.Translate(textEntry));

            if (TranslationExists)
            {
                Assert.NotEqual(CoreTools.Translate(textEntry), textEntry);
            }
            else
            {
                Assert.Equal(CoreTools.Translate(textEntry), textEntry);
            }

            Assert.Equal(CoreTools.AutoTranslated(textEntry), textEntry);
        }

        [Fact]
        public async Task TestWhichFunctionForExistingFile()
        {
            Tuple<bool, string> result = await CoreTools.Which("cmd.exe");
            Assert.True(result.Item1);
            Assert.True(File.Exists(result.Item2));
        }

        [Fact]
        public async Task TestWhichFunctionForNonExistingFile()
        {
            Tuple<bool, string> result = await CoreTools.Which("nonexistentfile.exe");
            Assert.False(result.Item1);
            Assert.Equal("", result.Item2);
        }

        [Theory]
        [InlineData("7zip19.00-helpEr", "7zip19.00 HelpEr")]
        [InlineData("packagename", "Packagename")]
        [InlineData("Packagename", "Packagename")]
        [InlineData("PackageName", "PackageName")]
        [InlineData("pacKagENaMe", "PacKagENaMe")]
        [InlineData("PACKAGENAME", "PACKAGENAME")]
        [InlineData("package-Name", "Package Name")]
        [InlineData("pub-name/pkg-version_2.00.portable", "Pkg Version 2.00")]
        [InlineData("!helloWorld", "!helloWorld")]
        [InlineData("@stylistic/eslint-plugin", "Eslint Plugin")]
        [InlineData("Flask-RESTful", "Flask RESTful")]
        public void TestFormatAsName(string id, string name)
        {
            Assert.Equal(name, CoreTools.FormatAsName(id));
        }

        [Theory]
        [InlineData(89)]
        [InlineData(33)]
        [InlineData(558)]
        [InlineData(12)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(64)]
        public void TestRandomStringGenerator(int length)
        {
            string string1 = CoreTools.RandomString(length);
            string string2 = CoreTools.RandomString(length);
            string string3 = CoreTools.RandomString(length);
            Assert.Equal(string1.Length, length);
            Assert.Equal(string2.Length, length);
            Assert.Equal(string3.Length, length);

            // Zero-lenghted strings are always equal
            // One-lenghted strings are likely to be equal
            if (length > 1)
            {
                Assert.NotEqual(string1, string2);
                Assert.NotEqual(string2, string3);
                Assert.NotEqual(string1, string3);
            }

            foreach (string s in new []{string1, string2, string3})
            {
                foreach (char c in s)
                {
                    Assert.True("abcdefghijklmnopqrstuvwxyz0123456789".Contains(c));
                }
            }
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("https://invalid.url.com/this/is/an/invalid.php?file=to_test&if=the&code_returns=zero", 0)]
        [InlineData("https://www.marticliment.com/unigetui/wingetui_size_test.txt", 460)]
        public async Task TestFileSizeLoader(string uri, long expectedSize)
        {
            double size = await CoreTools.GetFileSizeAsync(uri != "" ? new Uri(uri) : null);
            Assert.Equal(expectedSize / 1048576, size);
        }

        [Theory]
        [InlineData("1000.0", 1000.0, 0.0)]
        [InlineData("2.4", 2.4, 0.001)]
        [InlineData("33a.12-beta", 33.12, 0.0)]
        [InlineData("0", 0.0, 0.0)]
        [InlineData("", -1, 0.0)]
        [InlineData("dfgfdsgdfg", -1, 0.0)]
        [InlineData("-12", 12.0, 0.0)]
        [InlineData("4.0.0.1.0", 4.001, 0.01)]
        [InlineData("2024.30.04.1223", 2024.30041223, 0.0)]
        [InlineData("0.0", 0.0, 0.0)]
        public void TestGetVersionStringAsFloat(string version, double expected, double tolerance)
        {
            Assert.Equal(expected, CoreTools.GetVersionStringAsFloat(version), tolerance);
        }

        [Theory]
        [InlineData("Hello World", "Hello World")]
        [InlineData("Hello; World", "Hello World")]
        [InlineData("\"Hello; World\"", "Hello World")]
        [InlineData("'Hello; World'", "Hello World")]
        [InlineData("query\";start cmd.exe", "querystart cmd.exe")]
        [InlineData("query;start /B program.exe", "querystart B program.exe")]
        [InlineData(";&|<>%\"e'~?/\\`", "e")]
        public void TestSafeQueryString(string query, string expected)
        {
            Assert.Equal(CoreTools.EnsureSafeQueryString(query), expected);
        }
    }
}