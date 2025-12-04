# Feedback System Feature - Implementation Guide

## Overview

This feature implements an in-app feedback/issue reporting system for UniGetUI, addressing [Issue #2023](https://github.com/marticliment/UniGetUI/issues/2023).

## Features

### ✅ Implemented

1. **FeedbackService** (`src/UniGetUI/Services/FeedbackService.cs`)
   - Collects system information (OS, architecture, .NET version, etc.)
   - Gathers installed package managers and their status
   - Retrieves recent application logs
   - Formats data as GitHub-flavored markdown

2. **GitHubIssueHelper** (`src/UniGetUI/Services/GitHubIssueHelper.cs`)
   - Opens pre-filled GitHub issue creation page in browser
   - Supports different issue types (Bug, Feature, Enhancement)
   - Handles URL encoding for special characters

3. **FeedbackDialog** (`src/UniGetUI/Pages/Dialogs/FeedbackDialog.xaml`)
   - User-friendly WinUI 3 dialog
   - Issue type selection (Bug/Feature/Enhancement)
   - Title and description input
   - Toggles for including system info and logs
   - Live preview of generated issue body
   - Copy to clipboard functionality
   - Direct browser launch to GitHub

4. **Unit Tests** (`src/UniGetUI.Tests/Services/`)
   - Comprehensive test coverage for FeedbackService
   - GitHubIssueHelper validation tests
   - 30+ test cases covering all scenarios

## How It Works

### User Flow

1. **User Opens Feedback Dialog**
   - From Settings → Help & Feedback → Report Issue
   - Or via Help menu

2. **User Fills Form**
   - Selects issue type (Bug/Feature/Enhancement)
   - Enters title and description
   - Chooses whether to include system info and logs

3. **System Collects Data**
   - When user clicks "Generate Preview" or "Submit"
   - FeedbackService gathers:
     - UniGetUI version
     - Windows version and architecture
     - .NET runtime version
     - Installed package managers (WinGet, Chocolatey, Scoop, etc.)
     - Last 200 lines of application logs

4. **Issue Submission**
   - **Primary Button ("Submit on GitHub"):**
     - Copies full issue body to clipboard
     - Opens GitHub issue template in browser
     - User pastes clipboard content into GitHub form
   
   - **Secondary Button ("Copy to Clipboard"):**
     - Copies issue body to clipboard
     - User can manually create issue

### Generated Issue Format

```markdown
### Description

[User's description here]

### System Information

- **UniGetUI Version:** 3.1.0
- **OS Version:** Microsoft Windows NT 10.0.22631.0
- **OS Architecture:** x64
- **Processor Architecture:** AMD64
- **.NET Version:** 8.0.0
- **System Locale:** en-US
- **UI Locale:** en-US
- **Total Memory:** 16384 MB

### Package Managers

- ✅ **WinGet**
  - Version: 1.7.10582
  - Path: `C:\Program Files\WindowsApps\...\winget.exe`
- ✅ **Chocolatey**
  - Version: 2.2.2
  - Path: `C:\ProgramData\chocolatey\bin\choco.exe`
- ❌ **Scoop**

### Recent Application Logs

[2025-12-04 19:45:23] INFO: Application started
[2025-12-04 19:45:24] INFO: Package managers initialized
...
```

## Integration Points

### Where to Add Menu Items

#### 1. Settings Page

Add a "Send Feedback" button in the Help & Support section:

**File:** `src/UniGetUI/Pages/SettingsPage.xaml`

```xml
<StackPanel Orientation="Horizontal" Spacing="8">
    <Button Content="Report an Issue" Click="ReportIssue_Click">
        <Button.Icon>
            <FontIcon Glyph="&#xE8F2;"/>
        </Button.Icon>
    </Button>
</StackPanel>
```

**File:** `src/UniGetUI/Pages/SettingsPage.xaml.cs`

```csharp
using UniGetUI.Pages.Dialogs;

private async void ReportIssue_Click(object sender, RoutedEventArgs e)
{
    var dialog = new FeedbackDialog
    {
        XamlRoot = Content.XamlRoot
    };
    await dialog.ShowAsync();
}
```

#### 2. Help Menu (Main Window)

Add to the help menu or create a dedicated "Feedback" menu item:

**File:** `src/UniGetUI/MainWindow.xaml`

```xml
<MenuFlyoutItem Text="Send Feedback" Click="SendFeedback_Click">
    <MenuFlyoutItem.Icon>
        <FontIcon Glyph="&#xE8F2;"/>
    </MenuFlyoutItem.Icon>
</MenuFlyoutItem>
```

**File:** `src/UniGetUI/MainWindow.xaml.cs`

```csharp
using UniGetUI.Pages.Dialogs;

private async void SendFeedback_Click(object sender, RoutedEventArgs e)
{
    var dialog = new FeedbackDialog
    {
        XamlRoot = Content.XamlRoot
    };
    await dialog.ShowAsync();
}
```

#### 3. After Crash/Error (Optional)

Automatically show feedback dialog after critical errors:

**File:** `src/UniGetUI/CrashHandler.cs`

```csharp
using UniGetUI.Pages.Dialogs;

private async void ShowCrashReport(Exception ex)
{
    var dialog = new FeedbackDialog
    {
        XamlRoot = App.MainWindow.Content.XamlRoot
    };
    
    // TODO: Add method to pre-fill with error details
    // dialog.SetErrorContext(ex);
    
    await dialog.ShowAsync();
}
```

## Dependencies

### NuGet Packages Required

- Already included in project:
  - `Microsoft.Windows.SDK.Contracts`
  - `System.Text.Json`

### Existing Project References Used

- `UniGetUI.Core.Logging` - For logging
- `UniGetUI.Core.Tools` - For utility functions
- `UniGetUI.PackageEngine.PackageManagerClasses` - For package manager info

## Testing

### Running Tests

```bash
dotnet test src/UniGetUI.Tests/UniGetUI.Tests.csproj
```

### Test Coverage

**FeedbackServiceTests.cs** (18 tests)

- ✅ System information collection
- ✅ Markdown formatting
- ✅ Package manager detection
- ✅ Log retrieval
- ✅ Issue body creation with options
- ✅ Singleton pattern

**GitHubIssueHelperTests.cs** (8 tests)

- ✅ URL generation for all issue types
- ✅ Special character handling
- ✅ Edge cases (empty, long, null values)
- ✅ Invalid input handling

### Manual Testing Checklist

- [ ] Dialog opens successfully from menu
- [ ] Issue type selection works
- [ ] Title and description can be entered
- [ ] Generate Preview button works
- [ ] System info is collected correctly
- [ ] Package managers are detected
- [ ] Logs are retrieved (last 200 lines)
- [ ] Preview shows formatted markdown
- [ ] Submit button opens GitHub in browser
- [ ] Clipboard copy works
- [ ] Cancel button closes dialog
- [ ] Loading indicators work
- [ ] Error handling works (empty fields, etc.)

## Future Enhancements

### Phase 2 (Optional)

1. **Direct API Submission**
   - Implement OAuth flow for GitHub authentication
   - Allow users to submit issues directly via API
   - Store GitHub PAT securely

2. **Screenshot Capture**
   - Add "Attach Screenshot" button
   - Capture current app window
   - Upload to GitHub as attachment

3. **Log Filtering**
   - Allow users to select which logs to include
   - Filter sensitive information
   - Highlight relevant log entries

4. **Template Customization**
   - Different templates for different issue types
   - Guided form based on issue type
   - Required fields enforcement

5. **Crash Report Integration**
   - Automatically trigger feedback dialog on crashes
   - Pre-fill with exception details
   - Include stack trace

## Privacy & Security

### Data Collected

- System version and architecture
- Installed package managers
- Application logs (user can opt-out)
- No personal identifying information
- No network credentials

### User Control

- Users can preview all data before submission
- Option to exclude logs
- Option to exclude system info
- Copy to clipboard for manual review

## Troubleshooting

### Common Issues

**Issue:** Dialog doesn't open

- Check XamlRoot is set correctly
- Verify dialog is created on UI thread

**Issue:** Logs not appearing

- Check Logger.GetLogFilePath() returns valid path
- Verify log file exists and is readable

**Issue:** Package managers not detected

- Verify PackageEngine is initialized
- Check package manager status in main app

**Issue:** GitHub page doesn't open

- Check default browser is set
- Verify URL encoding is correct
- Check for special characters in title

**Issue:** Tests failing

- Ensure all project references are restored
- Check .NET 8 SDK is installed
- Verify test runner is compatible

## Contributing

To contribute to this feature:

1. Test the implementation thoroughly
2. Report any bugs or issues
3. Suggest UI/UX improvements
4. Help with translations for dialog text
5. Add more unit tests for edge cases
6. Improve error handling

## Architecture

### Class Diagram

```text
┌─────────────────────┐
│  FeedbackDialog     │
│  (XAML UI)          │
└──────────┬──────────┘
           │
           │ uses
           ▼
┌─────────────────────┐      ┌─────────────────────┐
│  FeedbackService    │◄─────┤ SystemInformation   │
│  (Singleton)        │      │ PackageManagerInfo  │
└──────────┬──────────┘      └─────────────────────┘
           │
           │ collaborates with
           ▼
┌─────────────────────┐
│ GitHubIssueHelper   │
│                     │
└─────────────────────┘
```

### Data Flow

```text
User Input → FeedbackDialog
    ↓
    ├─► FeedbackService.CollectSystemInfoAsync()
    │       ↓
    │       └─► SystemInformation + PackageManagerInfo
    │
    ├─► FeedbackService.CollectLogsAsync()
    │       ↓
    │       └─► Formatted log string
    │
    ├─► FeedbackService.CreateIssueBodyAsync()
    │       ↓
    │       └─► Complete markdown issue body
    │
    └─► GitHubIssueHelper.OpenIssuePage()
            ↓
            └─► Browser opens with pre-filled GitHub issue
```

## License

This feature is part of UniGetUI and follows the same license (MIT).
