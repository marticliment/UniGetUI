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
                "AutomationProperties.Name=\"{x:Bind ExpandCollapseOperationsAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ResizeOperationsAreaAutomationName, Mode=OneWay}\""
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
            ["src/UniGetUI/Pages/DialogPages/OperationFailedDialog.xaml"] =
            [
                "AutomationProperties.Name=\"Command-line output\""
            ],
            ["src/UniGetUI/Pages/DialogPages/OperationLiveLogPage.xaml"] =
            [
                "AutomationProperties.Name=\"Operation live log output\""
            ],
            ["src/UniGetUI/Pages/SoftwarePages/AbstractPackagesPage.xaml"] =
            [
                "AutomationProperties.Name=\"{x:Bind PackageOptionsAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ReloadPackagesAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind MoreToolbarActionsAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SelectPackageAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind OrderByAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ViewModeAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SearchPackagesAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SelectAllSourcesAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ClearSourceSelectionAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind InstantSearchAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind DistinguishUpperLowerCaseAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind IgnoreSpecialCharactersAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind ToggleFiltersAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind MainSelectionActionAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SearchModeOptionsAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SearchModeByNameAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SearchModeByIdAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SearchModeByBothAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SearchModeExactMatchAutomationName, Mode=OneWay}\"",
                "AutomationProperties.Name=\"{x:Bind SearchModeSimilarResultsAutomationName, Mode=OneWay}\""
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

    [Fact]
    public void AccessibilityCriticalCodePathsSetNamesAndTabFocus()
    {
        Dictionary<string, string[]> requiredSnippets = new()
        {
            ["src/UniGetUI/Controls/CustomNavViewItem.cs"] =
            [
                "AutomationProperties.SetName(this, text);"
            ],
            ["src/UniGetUI/Services/UserAvatar.cs"] =
            [
                "AutomationProperties.SetName(profileButton, CoreTools.Translate(\"Open backup profile\"));"
            ],
            ["src/UniGetUI/Controls/SettingsWidgets/SettingsPageButton.cs"] =
            [
                "AutomationProperties.SetName(this, name);",
                "IsTabStop = true;",
                "UseSystemFocusVisuals = true;"
            ],
            ["src/UniGetUI/Controls/SettingsWidgets/CheckboxCard.cs"] =
            [
                "AutomationProperties.SetName(_checkbox, name);",
                "AutomationProperties.SetName(this, name);"
            ],
            ["src/UniGetUI/Controls/SettingsWidgets/SecureCheckboxCard.cs"] =
            [
                "AutomationProperties.SetName(_checkbox, name);",
                "AutomationProperties.SetName(this, name);"
            ],
            ["src/UniGetUI/Controls/SettingsWidgets/CheckboxButtonCard.cs"] =
            [
                "AutomationProperties.SetName(_checkbox, _translatedCheckboxText);",
                "AutomationProperties.SetName(Button, buttonName);"
            ],
            ["src/UniGetUI/Controls/SettingsWidgets/ButtonCard.cs"] =
            [
                "AutomationProperties.SetName(_button, buttonName);",
                "AutomationProperties.SetName(this, cardName);"
            ],
            ["src/UniGetUI/Controls/SettingsWidgets/ComboboxCard.cs"] =
            [
                "AutomationProperties.SetName(_combobox, _translatedText);",
                "AutomationProperties.SetName(this, _translatedText);"
            ],
            ["src/UniGetUI/Controls/SettingsWidgets/TextboxCard.cs"] =
            [
                "AutomationProperties.SetName(_textbox, textboxName);",
                "AutomationProperties.SetName(this, textboxName);"
            ],
            ["src/UniGetUI/Pages/SettingsPages/SettingsBasePage.xaml.cs"] =
            [
                "BackButton.PreviewKeyDown += BackButton_PreviewKeyDown;",
                "root.TabFocusNavigation = KeyboardNavigationMode.Local;",
                "MainNavigationFrame.TabFocusNavigation = KeyboardNavigationMode.Local;",
                "scroller.TabFocusNavigation = KeyboardNavigationMode.Local;",
                "NeedsContextualName(currentName, toggle.OnContent, toggle.OffContent)",
                "AutomationProperties.SetName(card, cardName);"
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
}
