# Auto-Dismiss Feature for Failure Dialogs

## Overview

This feature automatically dismisses package operation failure dialogs after a configurable timeout period, addressing issue [#2097](https://github.com/marticliment/UniGetUI/issues/2097).

## Features

### 1. Auto-Dismiss Timer
- **Default timeout**: 10 seconds
- **Configurable range**: 3-60 seconds
- **Visual countdown**: Displays remaining time in an InfoBar
- **User-friendly**: Clear messaging about when dialog will close

### 2. Pause on Hover
- Timer automatically pauses when mouse hovers over the dialog
- Resumes countdown when mouse leaves
- Provides users time to read error details without interruption
- **Implementation**: Uses `timer.Stop()` on hover, `timer.Start()` on exit

### 3. Manual Control
- **"Keep Open" button**: Permanently cancels auto-dismiss for current dialog
- **Close button**: Manually closes dialog at any time
- **Retry button**: Retries operation and closes dialog
- **Implementation**: "Keep Open" sets timer to null

### 4. Settings Integration
- **Enable/Disable toggle**: Turn feature on/off globally
- **Timeout slider**: Adjust countdown duration (3-60 seconds)
- **Persistent**: Settings are saved and restored across sessions

## User Interface

### Countdown InfoBar
When auto-dismiss is active, an informational banner appears showing:
- Countdown message: "This dialog will close in X seconds"
- "Keep Open" button to cancel auto-dismiss

### Settings Page
Location: Settings → General → Interface

Controls:
- **Toggle**: "Auto-dismiss failure dialogs"
- **Slider**: "Auto-dismiss timeout" (3-60 seconds)
- **Description**: Explains the feature and hover-pause behavior

## Technical Implementation

### Key Design Decisions

#### State Management via Timer
Instead of using boolean flags for state tracking, the implementation uses the timer itself:

```csharp
private DispatcherTimer? _autoDismissTimer;  // Nullable = can represent "cancelled"
private int _remainingSeconds;
```

**Benefits:**
- **Cancelled state**: Represented by `_autoDismissTimer == null`
- **Hover state**: Managed by calling `timer.Stop()`/`timer.Start()`
- **Simpler logic**: No boolean flags to track and check
- **Less branching**: Timer tick handler only counts down

#### Simplified Configuration

```csharp
private int? GetAutoDismissTimeout()
{
    if (!Settings.Get(AUTO_DISMISS_ENABLED_SETTING, true))
        return null;  // Disabled = null timeout

    var timeout = Settings.Get(AUTO_DISMISS_TIMEOUT_SETTING, DEFAULT_AUTO_DISMISS_SECONDS);
    return Math.Clamp(timeout, 3, 60);  // Enforce 3-60 second range
}
```

**Benefits:**
- Single method combines enable check + timeout retrieval
- Returns `null` when disabled (no separate `IsEnabled()` method needed)
- Uses `Math.Clamp()` for clean range validation

#### Timer Lifecycle

**Initialization:**
```csharp
var timeout = GetAutoDismissTimeout();
if (timeout is not null)
{
    _remainingSeconds = timeout.Value;
    _autoDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
    _autoDismissTimer.Tick += AutoDismissTimer_Tick;
    _autoDismissTimer.Start();
}
```

**Tick Handler:**
```csharp
private void AutoDismissTimer_Tick(object? sender, object e)
{
    _remainingSeconds--;

    if (_remainingSeconds <= 0)
    {
        _autoDismissTimer?.Stop();
        _autoDismissTimer = null;
        CloseDialog();
    }
    else
    {
        UpdateAutoDismissDisplay();
    }
}
```

**No flag checks** - The handler simply counts down. Pausing is handled externally.

**Pause on Hover:**
```csharp
private void Page_PointerEntered(object sender, PointerRoutedEventArgs e)
{
    _autoDismissTimer?.Stop();  // Pause
}

private void Page_PointerExited(object sender, PointerRoutedEventArgs e)
{
    _autoDismissTimer?.Start();  // Resume
}
```

**Cancel (Keep Open):**
```csharp
private void KeepOpenButton_Click(object sender, RoutedEventArgs e)
{
    _autoDismissTimer?.Stop();
    _autoDismissTimer = null;  // Null = permanently cancelled
    AutoDismissInfoBar.IsOpen = false;
}
```

**Cleanup:**
```csharp
private void CloseDialog()
{
    _autoDismissTimer?.Stop();
    _autoDismissTimer = null;  // Prevent any restart
    Close?.Invoke(this, EventArgs.Empty);
}
```

### Complexity Reduction

**Removed:**
- ❌ `_autoDismissCancelled` boolean flag
- ❌ `_isHovered` boolean flag
- ❌ `IsAutoDismissEnabled()` method
- ❌ Branching in tick handler for flag checks
- ❌ `CloseButton_Click()` indirection method

**Result:**
- ✅ Cleaner state model (timer = source of truth)
- ✅ Fewer branches to reason about
- ✅ More maintainable code
- ✅ Same functionality, simpler implementation

## Testing

### Unit Tests
Location: `src/UniGetUI.Tests/OperationFailedDialogTests.cs`

Test coverage:
- ✅ `GetAutoDismissTimeout_WhenEnabled_ReturnsConfiguredTimeout`
- ✅ `GetAutoDismissTimeout_WhenDisabled_ReturnsNull`
- ✅ `GetAutoDismissTimeout_ClampsValueBetween3And60Seconds`
- ✅ `GetAutoDismissTimeout_DefaultIs10Seconds`
- ✅ `Dialog_WhenCreatedWithAutoDisabled_ShouldNotShowInfoBar`
- ✅ `Dialog_WhenCreatedWithAutoEnabled_ShouldShowInfoBar`
- ✅ `TimerState_RepresentsCancelledState` (documents design pattern)
- ✅ `HoverState_ManagedByTimerPauseResume` (documents design pattern)

### Manual Testing

1. **Enable auto-dismiss**
   - Go to Settings → General → Interface
   - Enable "Auto-dismiss failure dialogs"
   - Trigger a package operation failure
   - Verify countdown appears and dialog closes after timeout

2. **Hover pause**
   - Trigger failure dialog with auto-dismiss enabled
   - Move mouse over dialog during countdown
   - Verify countdown pauses (timer stops)
   - Move mouse away and verify countdown resumes (timer starts)

3. **Keep Open button**
   - Trigger failure dialog with auto-dismiss enabled
   - Click "Keep Open" button
   - Verify countdown stops and InfoBar disappears
   - Verify dialog stays open indefinitely
   - Verify hovering doesn't restart timer (timer is null)

4. **Custom timeout**
   - Change timeout setting to different values (e.g., 5, 30, 60)
   - Verify countdown uses configured duration

## User Benefits

### Problem Solved
When multiple package updates fail (e.g., 50 out of 100 packages), users previously had to manually close each failure dialog. With auto-dismiss:
- Dialogs automatically clear after a reasonable time
- Failed operations remain visible in Operation History
- Users can still access full error details when needed

### Usability Improvements
- **Reduced click fatigue**: No need to manually close dozens of dialogs
- **Better workflow**: Failures are acknowledged but don't block the UI indefinitely
- **Flexibility**: Users who want to review errors can hover or click "Keep Open"
- **Accessibility**: Clear countdown messaging and pause-on-hover

## Code Quality

### Simplicity Principles Applied

1. **State through structure, not flags**
   - Timer existence represents active/cancelled state
   - Timer running/stopped represents hover state
   - No boolean flag synchronization issues

2. **Single responsibility**
   - `GetAutoDismissTimeout()`: Configuration only
   - Tick handler: Countdown only
   - Hover handlers: Timer control only

3. **Null-safety**
   - Nullable timer (`DispatcherTimer?`)
   - Safe navigation (`timer?.Stop()`)
   - Explicit null checks where needed

## Localization

Translatable strings:
- "This dialog will close in X seconds"
- "This dialog will close in 1 second"
- "Keep Open"
- "Auto-dismiss failure dialogs" (settings)
- "Auto-dismiss timeout" (settings)
- "Timeout in seconds (3-60)" (settings description)

## Future Enhancements

Potential improvements for future versions:
- Sound notification before auto-dismiss
- Different timeouts for different failure types
- "Close All Failures" button for manual batch dismissal
- Statistics: Track most common failure types
- Smart timeout: Longer timeout for longer error messages
- Animation/fade-out effect before closing

## Credits

Implemented by: [@skanda890](https://github.com/skanda890)  
Issue: [#2097](https://github.com/marticliment/UniGetUI/issues/2097)  
Code review and simplification suggestions: Community feedback  
Discussion participants: @marticliment, @arthurcaccavo
