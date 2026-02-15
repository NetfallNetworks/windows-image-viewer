# Wallpaper Sync - Phase 3: UX Features & Settings GUI

**Epic**: User Experience & Production Readiness
**Priority**: HIGH
**Total Story Points**: 30
**Estimated Duration**: 5-7 days

---

## Phase Overview

Phase 3 adds the complete GUI-based configuration and user experience polish. This is where the "grandma test" is achieved - anyone can configure the app without technical knowledge or JSON editing.

**Key Objectives**:
1. Settings window with live preview (replaces JSON editing)
2. Welcome wizard for first-run experience
3. Enhanced tray icon with visual states
4. Enable/disable toggle for controlling updates
5. Startup toggle for Windows boot configuration
6. Reset to defaults functionality

---

## Story WS-9: Settings Window with Live Preview

**Story Points**: 8
**Type**: Feature / UI
**Priority**: CRITICAL

### Context

Currently, users must manually edit `WallpaperApp.json` to configure the app. This is error-prone and intimidating for non-technical users. Need a clean, simple GUI that replaces JSON editing entirely.

### Description

Create a WPF settings window with a single-page layout, real-time URL validation, file browser, fit mode selector, and refresh interval slider. Window should open on left-click of tray icon.

### Tasks

**Create New Files**:
- [ ] `src/WallpaperApp.TrayApp/Views/SettingsWindow.xaml`
- [ ] `src/WallpaperApp.TrayApp/Views/SettingsWindow.xaml.cs`
- [ ] `src/WallpaperApp.TrayApp/ViewModels/SettingsViewModel.cs`
- [ ] `src/WallpaperApp.TrayApp/Converters/FitModeToStringConverter.cs` (for ComboBox binding)

**Modify Existing Files**:
- [ ] `src/WallpaperApp.TrayApp/MainWindow.xaml.cs`:
  - Add left-click handler to `_notifyIcon`
  - Show `SettingsWindow` on click
  - Remove "Settings" from context menu (redundant - now left-click opens settings)
- [ ] `src/WallpaperApp.TrayApp/WallpaperApp.TrayApp.csproj`:
  - Add ViewModels folder to build
  - Add Converters folder to build

### Window Layout Specification

```
┌─────────────────────────────────────────────────┐
│  Wallpaper Sync - Settings          [_][□][X]  │
├─────────────────────────────────────────────────┤
│                                                  │
│  Image Source                                    │
│  ○ URL      ● Local File                        │
│                                                  │
│  [https://example.com/image.png  ] [Browse...]  │
│  ⓘ Enter HTTPS URL or select local image        │
│  [ERROR: Must use HTTPS]  ← Only shown if error │
│                                                  │
│  Refresh Interval (minutes)                     │
│  [15  ] ◄──────────────► [Slider 1-1440]       │
│                                                  │
│  Wallpaper Fit Mode                             │
│  [Fill (recommended) ▼]                         │
│  ⓘ How the image fits your screen               │
│                                                  │
│  ┌────────────────────────────────────────┐    │
│  │      [Preview - 200x150 thumbnail]     │    │
│  │      (Shows fit mode visualization)    │    │
│  └────────────────────────────────────────┘    │
│                                                  │
│  [Test Wallpaper] [Reset]   [Save] [Cancel]    │
└─────────────────────────────────────────────────┘
```

**Window Dimensions**: 550px wide × 650px tall, centered on screen

### Implementation Details

**SettingsViewModel.cs**:
```csharp
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IConfigurationService _configService;
    private readonly IAppStateService _stateService;
    private readonly IWallpaperService _wallpaperService;
    private readonly IImageValidator _imageValidator;

    // Bindable Properties
    private string _imageUrl;
    public string ImageUrl
    {
        get => _imageUrl;
        set
        {
            _imageUrl = value;
            OnPropertyChanged();
            ValidateUrl();
        }
    }

    private string _localImagePath;
    public string LocalImagePath
    {
        get => _localImagePath;
        set
        {
            _localImagePath = value;
            OnPropertyChanged();
            UpdatePreview();
        }
    }

    private ImageSource _sourceType;
    public ImageSource SourceType
    {
        get => _sourceType;
        set
        {
            _sourceType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUrlMode));
            OnPropertyChanged(nameof(IsLocalFileMode));
            ValidateUrl();
        }
    }

    public bool IsUrlMode => SourceType == ImageSource.Url;
    public bool IsLocalFileMode => SourceType == ImageSource.LocalFile;

    private int _refreshIntervalMinutes;
    public int RefreshIntervalMinutes
    {
        get => _refreshIntervalMinutes;
        set
        {
            _refreshIntervalMinutes = Math.Clamp(value, 1, 1440);
            OnPropertyChanged();
        }
    }

    private WallpaperFitMode _selectedFitMode;
    public WallpaperFitMode SelectedFitMode
    {
        get => _selectedFitMode;
        set
        {
            _selectedFitMode = value;
            OnPropertyChanged();
            UpdatePreview();
        }
    }

    private string? _urlValidationError;
    public string? UrlValidationError
    {
        get => _urlValidationError;
        set
        {
            _urlValidationError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUrlValid));
        }
    }

    public bool IsUrlValid => string.IsNullOrEmpty(UrlValidationError);

    // Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowseCommand { get; }
    public ICommand TestWallpaperCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }

    private void ValidateUrl()
    {
        if (SourceType != ImageSource.Url)
        {
            UrlValidationError = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(ImageUrl))
        {
            UrlValidationError = "URL is required";
            return;
        }

        if (!ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            UrlValidationError = "Must use HTTPS";
            return;
        }

        if (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out _))
        {
            UrlValidationError = "Invalid URL format";
            return;
        }

        UrlValidationError = null;
    }

    private void OnSave()
    {
        ValidateUrl();
        if (!IsUrlValid && SourceType == ImageSource.Url)
        {
            MessageBox.Show("Please fix validation errors before saving.",
                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = new AppSettings
        {
            ImageUrl = ImageUrl,
            LocalImagePath = LocalImagePath,
            SourceType = SourceType,
            RefreshIntervalMinutes = RefreshIntervalMinutes,
            FitMode = SelectedFitMode
        };

        // Save to JSON
        _configService.SaveConfiguration(settings);

        MessageBox.Show("Settings saved successfully.", "Success",
            MessageBoxButton.OK, MessageBoxImage.Information);

        // Close window
        CloseWindow?.Invoke();
    }

    private void OnBrowse()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Title = "Select Wallpaper Image"
        };

        if (dialog.ShowDialog() == true)
        {
            // Validate using ImageValidator
            if (_imageValidator.IsValidImage(dialog.FileName, out var format))
            {
                LocalImagePath = dialog.FileName;
            }
            else
            {
                MessageBox.Show(
                    "The selected file is not a valid image. Only PNG, JPG, and BMP are supported.",
                    "Invalid Image", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnTestWallpaper()
    {
        try
        {
            string imagePath = SourceType == ImageSource.Url
                ? ImageUrl
                : LocalImagePath;

            if (SourceType == ImageSource.LocalFile)
            {
                _wallpaperService.SetWallpaper(imagePath, SelectedFitMode);
                MessageBox.Show("Wallpaper set successfully!", "Test Wallpaper",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "Test wallpaper is only available for local files. Save your settings to test URL mode.",
                    "Test Wallpaper", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to set wallpaper: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnReset()
    {
        var result = MessageBox.Show(
            "Reset all settings to defaults?\n\nThis will:\n" +
            "- Clear image URL/path\n" +
            "- Reset refresh interval to 15 minutes\n" +
            "- Set fit mode to Fill\n" +
            "- Clear last-known-good image",
            "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ImageUrl = string.Empty;
            LocalImagePath = string.Empty;
            RefreshIntervalMinutes = 15;
            SelectedFitMode = WallpaperFitMode.Fill;
            SourceType = ImageSource.Url;

            // Clear state
            var state = new AppState { IsFirstRun = false, IsEnabled = true };
            _stateService.SaveState(state);

            MessageBox.Show("Settings reset to defaults.", "Reset Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UpdatePreview()
    {
        // Simple preview: show placeholder text describing fit mode
        // Full image preview is complex, defer to future enhancement
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public Action? CloseWindow { get; set; }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

**SettingsWindow.xaml** (key parts):
```xaml
<Window x:Class="WallpaperApp.TrayApp.Views.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="Wallpaper Sync - Settings"
        Width="550" Height="650"
        WindowStartupLocation="CenterScreen">
    <Window.DataContext>
        <local:SettingsViewModel />
    </Window.DataContext>

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Image Source Selection -->
        <StackPanel Grid.Row="0">
            <TextBlock Text="Image Source" FontWeight="Bold" Margin="0,0,0,10"/>
            <StackPanel Orientation="Horizontal">
                <RadioButton Content="URL" IsChecked="{Binding IsUrlMode}"
                             Command="{Binding SelectUrlModeCommand}" Margin="0,0,20,0"/>
                <RadioButton Content="Local File" IsChecked="{Binding IsLocalFileMode}"
                             Command="{Binding SelectLocalFileModeCommand}"/>
            </StackPanel>
        </StackPanel>

        <!-- URL/File Path Input -->
        <StackPanel Grid.Row="1" Margin="0,20,0,0">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBox Text="{Binding ImageUrl, UpdateSourceTrigger=PropertyChanged}"
                         Visibility="{Binding IsUrlMode, Converter={StaticResource BoolToVisibilityConverter}}"
                         Margin="0,0,10,0"/>
                <TextBox Text="{Binding LocalImagePath}"
                         IsReadOnly="True"
                         Visibility="{Binding IsLocalFileMode, Converter={StaticResource BoolToVisibilityConverter}}"
                         Margin="0,0,10,0"/>

                <Button Content="Browse..." Grid.Column="1"
                        Command="{Binding BrowseCommand}"
                        Visibility="{Binding IsLocalFileMode, Converter={StaticResource BoolToVisibilityConverter}}"/>
            </Grid>

            <!-- Validation Error -->
            <TextBlock Text="{Binding UrlValidationError}"
                       Foreground="Red"
                       Visibility="{Binding UrlValidationError, Converter={StaticResource NullToVisibilityConverter}}"
                       Margin="0,5,0,0"/>
        </StackPanel>

        <!-- Refresh Interval Slider -->
        <StackPanel Grid.Row="2" Margin="0,20,0,0">
            <TextBlock Text="Refresh Interval (minutes)" FontWeight="Bold"/>
            <Grid Margin="0,10,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <TextBox Text="{Binding RefreshIntervalMinutes}" Width="60"/>
                <Slider Grid.Column="1" Minimum="1" Maximum="1440"
                        Value="{Binding RefreshIntervalMinutes}"
                        TickFrequency="15" IsSnapToTickEnabled="True"
                        Margin="10,0,0,0"/>
            </Grid>
        </StackPanel>

        <!-- Fit Mode Selector -->
        <StackPanel Grid.Row="3" Margin="0,20,0,0">
            <TextBlock Text="Wallpaper Fit Mode" FontWeight="Bold"/>
            <ComboBox SelectedItem="{Binding SelectedFitMode}" Margin="0,10,0,0">
                <ComboBoxItem>Fill (recommended)</ComboBoxItem>
                <ComboBoxItem>Fit</ComboBoxItem>
                <ComboBoxItem>Stretch</ComboBoxItem>
                <ComboBoxItem>Tile</ComboBoxItem>
                <ComboBoxItem>Center</ComboBoxItem>
            </ComboBox>
        </StackPanel>

        <!-- Preview (placeholder for now) -->
        <Border Grid.Row="4" BorderBrush="#CCC" BorderThickness="1"
                Margin="0,20,0,0" Background="#F5F5F5">
            <TextBlock Text="Preview" HorizontalAlignment="Center"
                       VerticalAlignment="Center" Foreground="#999"/>
        </Border>

        <!-- Action Buttons -->
        <StackPanel Grid.Row="5" Orientation="Horizontal"
                    HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="Test Wallpaper" Command="{Binding TestWallpaperCommand}"
                    Margin="0,0,10,0" Padding="15,5"/>
            <Button Content="Reset" Command="{Binding ResetToDefaultsCommand}"
                    Margin="0,0,10,0" Padding="15,5"/>
            <Button Content="Cancel" Command="{Binding CancelCommand}"
                    Margin="0,0,10,0" Padding="15,5"/>
            <Button Content="Save" Command="{Binding SaveCommand}"
                    Padding="15,5" FontWeight="Bold"/>
        </StackPanel>
    </Grid>
</Window>
```

**Update MainWindow.xaml.cs** (add left-click handler):
```csharp
// In MainWindow constructor, after _notifyIcon creation:
_notifyIcon.Click += OnTrayIconClick;

private void OnTrayIconClick(object sender, EventArgs e)
{
    var settingsWindow = new SettingsWindow(_configService, _stateService,
                                            _wallpaperService, _imageValidator);
    settingsWindow.Show();
    settingsWindow.Activate(); // Bring to front
}
```

### Acceptance Criteria

- [ ] Settings window opens on left-click of tray icon
- [ ] URL validation shows errors in real-time (on blur/typing)
- [ ] Browse button filters to .png/.jpg/.bmp only
- [ ] Browse button validates file with ImageValidator
- [ ] Refresh interval slider constrained to 1-1440 minutes
- [ ] Fit mode ComboBox has all 5 options
- [ ] Save button writes settings to JSON correctly
- [ ] Cancel button closes window without saving
- [ ] Test Wallpaper button sets wallpaper immediately (local files only)
- [ ] Reset button clears all settings after confirmation
- [ ] All fields load current settings on window open
- [ ] Window is modal (blocks tray icon clicks while open)

### Testing Requirements

**Manual Testing Checklist**:
- [ ] Left-click tray icon → Settings window opens
- [ ] Enter invalid URL (http://) → Red error text appears
- [ ] Fix URL (https://) → Error text disappears
- [ ] Click Browse → Only image files (.png/.jpg/.bmp) selectable
- [ ] Select invalid file (fake .png) → Error dialog appears
- [ ] Select valid file → Path appears in textbox
- [ ] Change refresh interval slider → TextBox updates
- [ ] Type in refresh interval → Slider updates
- [ ] Test values outside range (0, 2000) → Clamped to 1-1440
- [ ] Select each fit mode → ComboBox updates
- [ ] Click Test Wallpaper (local file) → Wallpaper changes
- [ ] Click Save → Settings persist after app restart
- [ ] Click Cancel → Changes discarded
- [ ] Click Reset → Confirmation dialog → Settings cleared

### Definition of Done

- [x] Window layout matches specification
- [x] All validation rules enforced
- [x] Settings persist to JSON correctly
- [x] Manual test checklist 100% complete
- [x] No console errors or exceptions
- [x] Window keyboard navigation works (Tab order correct)

---

## Story WS-10: Welcome Wizard (First-Run Experience)

**Story Points**: 5
**Type**: Feature / UI
**Priority**: HIGH

### Description

Create a simple 2-page wizard that appears on first run, guiding new users through initial setup with a demo URL pre-filled.

### Tasks

- [ ] Create `src/WallpaperApp.TrayApp/Views/WelcomeWizard.xaml`
- [ ] Create `src/WallpaperApp.TrayApp/Views/WelcomeWizard.xaml.cs`
- [ ] Create `src/WallpaperApp.TrayApp/ViewModels/WelcomeViewModel.cs`
- [ ] Add first-run check to `MainWindow.xaml.cs` startup
- [ ] Show wizard if `AppState.IsFirstRun == true`
- [ ] Mark `IsFirstRun = false` when wizard completes
- [ ] Pre-fill demo URL: `https://source.unsplash.com/random/1920x1080`

### Wizard Page Flow

**Page 1: Welcome**
```
┌──────────────────────────────────────┐
│  Welcome to Wallpaper Sync!          │
│                                       │
│  Automatically update your desktop   │
│  wallpaper from a URL or local file. │
│                                       │
│  Features:                            │
│  • Automatic refresh                 │
│  • Multiple fit modes                │
│  • Offline fallback                  │
│  • Easy configuration                │
│                                       │
│             [Get Started >]           │
└──────────────────────────────────────┘
```

**Page 2: Quick Setup**
```
┌──────────────────────────────────────┐
│  Quick Setup                          │
│                                       │
│  Image URL (demo pre-filled):        │
│  [https://source.unsplash.com/...]   │
│                                       │
│  Refresh every: [15] minutes          │
│                                       │
│  You can change these later in        │
│  Settings (left-click tray icon).    │
│                                       │
│         [< Back]  [Finish]            │
└──────────────────────────────────────┘
```

**Window Size**: 450px wide × 400px tall, centered

### Implementation Details

**WelcomeViewModel.cs**:
```csharp
public class WelcomeViewModel : INotifyPropertyChanged
{
    private int _currentPage = 0;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPage1));
            OnPropertyChanged(nameof(IsPage2));
        }
    }

    public bool IsPage1 => CurrentPage == 0;
    public bool IsPage2 => CurrentPage == 1;

    public string DemoUrl { get; set; } = "https://source.unsplash.com/random/1920x1080";
    public int RefreshIntervalMinutes { get; set; } = 15;

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand FinishCommand { get; }

    private void OnNext()
    {
        if (CurrentPage < 1)
            CurrentPage++;
    }

    private void OnBack()
    {
        if (CurrentPage > 0)
            CurrentPage--;
    }

    private void OnFinish()
    {
        // Save settings
        var settings = new AppSettings
        {
            ImageUrl = DemoUrl,
            RefreshIntervalMinutes = RefreshIntervalMinutes,
            SourceType = ImageSource.Url,
            FitMode = WallpaperFitMode.Fill
        };
        _configService.SaveConfiguration(settings);

        // Mark first run complete
        _stateService.MarkFirstRunComplete();

        CloseWindow?.Invoke();
    }
}
```

**Update MainWindow.xaml.cs** (startup check):
```csharp
private void OnStartup(object sender, StartupEventArgs e)
{
    // ... existing initialization ...

    var state = _appStateService.LoadState();
    if (state.IsFirstRun)
    {
        var wizard = new WelcomeWizard(_configService, _stateService);
        wizard.ShowDialog(); // Modal - blocks until wizard completes
    }

    StartWallpaperUpdates();
}
```

### Acceptance Criteria

- [ ] Wizard appears only on first run
- [ ] Page 1 shows welcome message and features
- [ ] Page 2 pre-fills demo URL
- [ ] Back button navigates to previous page
- [ ] Finish button saves settings and closes wizard
- [ ] `IsFirstRun` flag set to false after Finish
- [ ] Wizard never appears again after completion
- [ ] User can edit demo URL before finishing

### Testing Requirements

**Manual Testing**:
- [ ] Delete `%LOCALAPPDATA%\WallpaperSync\state.json`
- [ ] Restart app → Wizard appears
- [ ] Click "Get Started" → Page 2 appears
- [ ] Verify demo URL pre-filled
- [ ] Click "Back" → Page 1 appears
- [ ] Click "Get Started" → "Finish" → Wizard closes
- [ ] Check `state.json`: `IsFirstRun: false`
- [ ] Restart app → Wizard does NOT appear

### Definition of Done

- [x] Wizard appears only once (first run)
- [x] All navigation works (Next, Back, Finish)
- [x] Settings saved correctly
- [x] Manual test checklist complete
- [x] Never appears again after completion

---

## Story WS-11: Tray Icon Enhancements

**Story Points**: 4
**Type**: Feature / UI
**Priority**: HIGH

### Description

Add visual states to tray icon (enabled = blue "W", disabled = gray "W") and improve context menu organization.

### Tasks

- [ ] Create `CreateTrayIcon(bool isEnabled)` method
- [ ] Generate icon dynamically with color based on state:
  - Enabled: Blue (#0078D4)
  - Disabled: Gray (#808080)
- [ ] Update icon when state changes
- [ ] Update tooltip when disabled: "Wallpaper Sync (Disabled)"
- [ ] Reorganize context menu:
  - Remove "Settings" menu item (redundant - left-click opens settings)
  - Add separator after "Enabled" toggle

### Implementation Details

**Enhanced CreateTrayIcon()**:
```csharp
private DrawingIcon CreateTrayIcon(bool isEnabled)
{
    var bitmap = new Bitmap(32, 32);
    using (var g = Graphics.FromImage(bitmap))
    {
        var bgColor = isEnabled
            ? Color.FromArgb(0, 120, 212)  // Blue (#0078D4)
            : Color.FromArgb(128, 128, 128); // Gray (#808080)

        g.Clear(bgColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var font = new DrawingFont("Segoe UI", 20, DrawingFontStyle.Bold);
        g.DrawString("W", font, Brushes.White, new PointF(4, 2));
    }

    return DrawingIcon.FromHandle(bitmap.GetHicon());
}

private void UpdateTrayIcon(bool isEnabled)
{
    _notifyIcon.Icon?.Dispose();
    _notifyIcon.Icon = CreateTrayIcon(isEnabled);
    _notifyIcon.Text = isEnabled
        ? "Wallpaper Sync"
        : "Wallpaper Sync (Disabled)";
}
```

**Context Menu Layout**:
```
✓ Enabled              ← Toggle (checkmark when enabled)
──────────────────     ← Separator
Refresh Now
──────────────────
About
Exit
```

### Acceptance Criteria

- [ ] Icon is blue when enabled, gray when disabled
- [ ] Tooltip shows "(Disabled)" when inactive
- [ ] Icon updates immediately when state changes
- [ ] "Settings" removed from context menu (left-click opens settings)
- [ ] Menu layout clean and organized

### Testing Requirements

**Manual Testing**:
- [ ] Start app (enabled) → Icon is blue
- [ ] Right-click → Uncheck "Enabled" → Icon turns gray
- [ ] Hover over icon → Tooltip shows "(Disabled)"
- [ ] Re-enable → Icon turns blue, tooltip normal
- [ ] Left-click icon → Settings opens (no menu)

### Definition of Done

- [x] Visual states work correctly
- [x] Tooltip updates with state
- [x] Icon changes are immediate
- [x] Manual test checklist complete

---

## Story WS-12: Enable/Disable Toggle

**Story Points**: 3
**Type**: Feature
**Priority**: HIGH

### Description

Add "Enabled" toggle to tray menu that pauses/resumes automatic wallpaper updates.

### Tasks

- [ ] Add `IsEnabled` property to AppState (already exists from WS-2)
- [ ] Add "Enabled" menu item to context menu (checkbox style)
- [ ] Implement `OnToggleEnabled()` handler
- [ ] Stop timer when disabled, restart when enabled
- [ ] Show balloon notification on state change
- [ ] Persist state across app restarts

### Implementation Details

**Add to MainWindow.xaml.cs**:
```csharp
private void BuildContextMenu()
{
    var contextMenu = new ContextMenuStrip();

    // Enable/Disable Toggle
    var toggleItem = new ToolStripMenuItem("Enabled");
    toggleItem.CheckOnClick = true;
    toggleItem.Checked = _appStateService.LoadState().IsEnabled;
    toggleItem.Click += OnToggleEnabled;
    contextMenu.Items.Add(toggleItem);

    contextMenu.Items.Add(new ToolStripSeparator());

    // ... rest of menu items ...

    // Update checkmark when menu opens
    contextMenu.Opening += (s, e) =>
    {
        var state = _appStateService.LoadState();
        toggleItem.Checked = state.IsEnabled;
    };

    _notifyIcon.ContextMenuStrip = contextMenu;
}

private void OnToggleEnabled(object sender, EventArgs e)
{
    var state = _appStateService.LoadState();
    state.IsEnabled = !state.IsEnabled;
    _appStateService.SaveState(state);

    if (state.IsEnabled)
    {
        StartWallpaperUpdates();
        ShowBalloonTip("Wallpaper Sync Enabled",
            "Automatic updates resumed", ToolTipIcon.Info);
    }
    else
    {
        StopWallpaperUpdates();
        ShowBalloonTip("Wallpaper Sync Disabled",
            "Automatic updates paused", ToolTipIcon.Info);
    }

    UpdateTrayIcon(state.IsEnabled);
}

private void StopWallpaperUpdates()
{
    _updateTimer?.Dispose();
    _updateTimer = null;
}
```

### Acceptance Criteria

- [ ] Menu item has checkbox that reflects state
- [ ] Clicking toggles between enabled/disabled
- [ ] Disabled state stops automatic updates
- [ ] Enabled state resumes updates
- [ ] State persists across restarts
- [ ] Balloon notification shows on toggle

### Testing Requirements

**Manual Testing**:
- [ ] Right-click → Uncheck "Enabled"
- [ ] Wait 15 minutes → No wallpaper update
- [ ] Restart app → Still disabled
- [ ] Right-click → Check "Enabled"
- [ ] Wallpaper updates immediately
- [ ] Balloon notification appears

### Definition of Done

- [x] Toggle works correctly
- [x] State persists
- [x] Manual test checklist complete

---

## Story WS-13: Startup Toggle Service

**Story Points**: 3
**Type**: Feature
**Priority**: MEDIUM

### Description

Add "Run at Startup" toggle to context menu that creates/removes startup shortcut.

### Tasks

- [ ] Create `src/WallpaperApp/Services/IStartupService.cs`
- [ ] Create `src/WallpaperApp/Services/StartupService.cs`
- [ ] Implement using IWshShortcut COM interface
- [ ] Add menu item to context menu
- [ ] Update checkmark dynamically when menu opens

### Implementation Details

**StartupService.cs**:
```csharp
public class StartupService : IStartupService
{
    private const string APP_NAME = "Wallpaper Sync";

    public bool IsStartupEnabled()
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startupFolder, $"{APP_NAME}.lnk");
        return File.Exists(shortcutPath);
    }

    public void EnableStartup()
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startupFolder, $"{APP_NAME}.lnk");
        string targetPath = Process.GetCurrentProcess().MainModule.FileName;

        var shell = new WshShell();
        var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
        shortcut.Description = "Wallpaper Sync - Automatic wallpaper updates";
        shortcut.Save();
    }

    public void DisableStartup()
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startupFolder, $"{APP_NAME}.lnk");

        if (File.Exists(shortcutPath))
            File.Delete(shortcutPath);
    }
}
```

**Add to context menu**:
```csharp
var startupItem = new ToolStripMenuItem("Run at Startup");
startupItem.CheckOnClick = true;
startupItem.Click += OnToggleStartup;
contextMenu.Items.Add(startupItem);

contextMenu.Opening += (s, e) =>
{
    startupItem.Checked = _startupService.IsStartupEnabled();
};

private void OnToggleStartup(object sender, EventArgs e)
{
    if (_startupService.IsStartupEnabled())
        _startupService.DisableStartup();
    else
        _startupService.EnableStartup();
}
```

### Acceptance Criteria

- [ ] Menu item reflects current startup state
- [ ] Enabling creates shortcut in Startup folder
- [ ] Disabling removes shortcut
- [ ] Reboot test: app starts automatically when enabled
- [ ] Shortcut name is "Wallpaper Sync.lnk"

### Testing Requirements

**Manual Testing**:
- [ ] Right-click → Check "Run at Startup"
- [ ] Check `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\`
- [ ] Shortcut "Wallpaper Sync.lnk" exists
- [ ] Reboot machine → App starts automatically
- [ ] Right-click → Uncheck "Run at Startup"
- [ ] Shortcut removed from Startup folder
- [ ] Reboot → App does NOT start

### Definition of Done

- [x] Startup toggle works
- [x] Reboot test passed
- [x] Manual test checklist complete

---

## Story WS-14: Reset to Defaults

**Story Points**: 2
**Type**: Feature
**Priority**: LOW

### Description

Add "Reset to Defaults" button in Settings window that clears all configuration and state.

### Tasks

- [ ] Add "Reset" button to Settings window (already in WS-9 layout)
- [ ] Implement reset logic in SettingsViewModel
- [ ] Show confirmation dialog
- [ ] Clear `WallpaperApp.json` (revert to defaults)
- [ ] Clear `state.json` (except `IsFirstRun = false`)
- [ ] Show success message

### Implementation (Already included in WS-9)

This story is mostly complete as part of WS-9. Just verify:
- [ ] Reset button exists
- [ ] Confirmation dialog shows
- [ ] Settings cleared correctly
- [ ] State cleared (except IsFirstRun)

### Acceptance Criteria

- [ ] Reset button shows confirmation dialog
- [ ] Clicking "Yes" clears all settings
- [ ] Success message displays
- [ ] Settings window shows defaults after reset
- [ ] First-run wizard does NOT appear after reset

### Definition of Done

- [x] Reset functionality works
- [x] Confirmation required
- [x] Manual test passed

---

## Phase 3 Complete Checklist

When all 6 stories are done:

- [ ] Settings window opens on left-click
- [ ] URL validation prevents errors
- [ ] Welcome wizard appears on first run only
- [ ] Tray icon shows enabled/disabled state
- [ ] Enable/disable toggle pauses updates
- [ ] Startup toggle persists across reboots
- [ ] Reset to defaults works correctly
- [ ] **Grandma Test**: Non-technical user can configure without help
- [ ] All manual tests pass
- [ ] No regressions in existing functionality

**Final Acceptance Test (Grandma Test)**:
```
Recruit a non-technical user (parent, grandparent, etc.)
1. Give them the app with no instructions
2. Ask: "Can you make this app show a picture on your desktop?"
3. Observe: Can they complete it in <5 minutes?
4. Success criteria:
   - They figure out to left-click the tray icon
   - They understand the settings window
   - They successfully set a wallpaper (URL or local file)
   - They don't ask for help or get frustrated
```

If the grandma test fails, the UX needs improvement.

---

**END OF PHASE 3 STORIES**
