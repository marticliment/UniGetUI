using System.Text.RegularExpressions;

namespace UniGetUI.Core.Tools.Tests;

public class AccessibilityXamlTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\..\.."));

    private static string ReadFile(string relativePath)
    {
        string path = Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }

    [Fact]
    public void IconOnlyControlsHaveAutomationNames()
    {
        Dictionary<string, string[]> requiredSnippets = new()
        {
            ["src/UniGetUI/Controls/DialogCloseButton.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind CloseAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Controls/SourceManager.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind RemoveSourceAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ReloadSourcesAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Pages/HelpPage.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind HelpBackAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind HelpForwardAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind HelpHomeAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind HelpReloadAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Pages/MainView.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind OperationOptionsAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind OperationsListActionsAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ExpandCollapseOperationsAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Pages/SettingsPages/SettingsBasePage.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind BackAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Pages/DialogPages/DesktopShortcuts.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind DeleteShortcutAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind OpenShortcutLocationAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind RemoveShortcutFromListAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Pages/DialogPages/IgnoredUpdates.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind StopIgnoringUpdatesAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Pages/DialogPages/PackageDetailsPage.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind MorePackageActionsAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Pages/SoftwarePages/AbstractPackagesPage.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind PackageOptionsAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ReloadPackagesAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind MoreToolbarActionsAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SelectPackageAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind OrderByAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ViewModeAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SearchPackagesAutomationName, Mode=OneWay}\""
            ],
            ["src/UniGetUI/Pages/SettingsPages/GeneralPages/Backup.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind ResetBackupDirectoryAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind OpenBackupDirectoryAutomationName, Mode=OneWay}\""
            ]
        };

        foreach (var (file, snippets) in requiredSnippets)
        {
            string content = ReadFile(file);
            foreach (string snippet in snippets)
            {
                Assert.Contains(snippet, content);
            }
        }
    }

    [Fact]
    public void CriticalListTemplatesHaveAutomationNames()
    {
        Dictionary<string, string[]> requiredSnippets = new()
        {
            ["src/UniGetUI/Controls/SourceManager.xaml"] =
            [
                "<ItemContainer AutomationProperties.Name=\"{x:Bind Source.Name}\">"
            ],
            ["src/UniGetUI/Pages/AboutPages/Contributors.xaml"] =
            [
                "<ItemContainer AutomationProperties.Name=\"{x:Bind Name}\">"
            ],
            ["src/UniGetUI/Pages/AboutPages/Translators.xaml"] =
            [
                "<ItemContainer AutomationProperties.Name=\"{x:Bind Name}\">"
            ],
            ["src/UniGetUI/Pages/AboutPages/ThirdPartyLicenses.xaml"] =
            [
                "<ItemContainer AutomationProperties.Name=\"{x:Bind Name}\">"
            ],
            ["src/UniGetUI/Pages/DialogPages/DesktopShortcuts.xaml"] =
            [
                "<ItemContainer AutomationProperties.Name=\"{x:Bind Name}\">"
            ],
            ["src/UniGetUI/Pages/DialogPages/IgnoredUpdates.xaml"] =
            [
                "<ItemContainer AutomationProperties.Name=\"{x:Bind Name}\">"
            ]
        };

        foreach (var (file, snippets) in requiredSnippets)
        {
            string content = ReadFile(file);
            foreach (string snippet in snippets)
            {
                Assert.Contains(snippet, content);
            }
        }
    }

    [Fact]
    public void CriticalItemContainersAreNamed()
    {
        string[] files =
        [
            "src/UniGetUI/Controls/SourceManager.xaml",
            "src/UniGetUI/Pages/AboutPages/Contributors.xaml",
            "src/UniGetUI/Pages/AboutPages/Translators.xaml",
            "src/UniGetUI/Pages/AboutPages/ThirdPartyLicenses.xaml",
            "src/UniGetUI/Pages/DialogPages/DesktopShortcuts.xaml",
            "src/UniGetUI/Pages/DialogPages/IgnoredUpdates.xaml",
            "src/UniGetUI/Pages/MainView.xaml"
        ];

        foreach (string file in files)
        {
            string content = ReadFile(file);
            MatchCollection matches = Regex.Matches(content, "<ItemContainer[^>]*>", RegexOptions.Singleline);
            Assert.NotEmpty(matches);

            foreach (Match match in matches)
            {
                Assert.Contains("AutomationProperties.Name", match.Value);
            }
        }
    }
}
