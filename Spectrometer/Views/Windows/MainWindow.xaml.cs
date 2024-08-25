using Spectrometer.Helpers;
using Spectrometer.Models;
using Spectrometer.ViewModels.Windows;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Windows;

public partial class MainWindow : INavigationWindow
{
    // ------------------------------------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------------------------------------

    public MainWindowViewModel ViewModel { get; }

    // ------------------------------------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------------------------------------

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewModel"></param>
    /// <param name="pageService"></param>
    /// <param name="navigationService"></param>
    public MainWindow(
        MainWindowViewModel viewModel,
        IPageService pageService,
        INavigationService navigationService)
    {
        ViewModel = viewModel;
        DataContext = this;

        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        SetPageService(pageService);

        navigationService.SetNavigationControl(RootNavigation);

        InitializeAsync();
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private async void InitializeAsync()
    {
        await CheckForUpdatesAsync();
        await InstallAppAsync();
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Check for App Updates
    /// Is the auto-update setting true, and have we not asked in the last 48 hours?
    /// </summary>
    private async Task CheckForUpdatesAsync()
    {
        bool automaticallyCheckForUpdates = App.SettingsMgr?.Settings?.AutomaticallyCheckForUpdates ?? true;

        DateTime? lastUpdateDefer = App.SettingsMgr?.Settings?.LastUpdateDefer;
        double hoursSinceLastDefer = lastUpdateDefer.HasValue ? (DateTime.Now - lastUpdateDefer.Value).TotalHours : 0;

        // -------------------------------------------------------------------------------------------
        // Check for updates if auto-update is enabled
        // .. and LastUpdateDefer is either default value (minvalue) or greater than 48 hours ago

        if (automaticallyCheckForUpdates
            && (lastUpdateDefer == DateTime.MinValue || hoursSinceLastDefer > 48))
        {
            AppUpdateManager updateManager = new();
            if (!await updateManager.IsUpdateAvailableAsync())
                return;

            // -------------------------------------------------------------------------------------------
            // Show "update available" dialog

            TransitionBehavior.ApplyFadeInTransition(RootContentDialog);

            ContentDialogService contentDialogService = new();
            contentDialogService.SetDialogHost(RootContentDialog);

            var contentDialogResult = await contentDialogService.ShowAsync(
                new ContentDialog()
                {
                    Title = "Update Available",
                    Content = "An update for Spectrometer is available.\n\nWould you like to download and install the update now?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "Remind Me Later",
                    IsSecondaryButtonEnabled = false,
                    DialogHeight = 240,
                    DialogMaxHeight = 240
                },
                CancellationToken.None
            );

            // -------------------------------------------------------------------------------------------
            // Perform update if Yes; defer for 48 hours if Later

            if (contentDialogResult == ContentDialogResult.Primary)
            {
                await updateManager.DownloadAndInstallLatestVersionAsync();
            }
            else if (App.SettingsMgr?.Settings is not null)
            {
                App.SettingsMgr.Settings.LastUpdateDefer = DateTime.Now;
                App.SettingsMgr.SaveSettings();

                Logger.Write("User deferred update; asking again in 48 hours");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Skip update check

        else
        {
            if (!automaticallyCheckForUpdates)
                Logger.Write("Skipping update check; auto-update is disabled");
            else if (hoursSinceLastDefer < 48)
                Logger.Write("Skipping update check; last check was less than 48 hours ago");
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Checks if the application is located in the Downloads folder, 
    /// and prompts the user to move it to AppData\Roaming if it is.
    /// </summary>
    private async Task InstallAppAsync()
    {
        if (App.SettingsMgr?.Settings?.IsInstallDeclined ?? false)
            return; // user has declined pseudo-install (or we don't have settings yet)

        string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        if (!currentExePath.StartsWith(downloadsPath, StringComparison.OrdinalIgnoreCase))
            return; // nothing to do

        Logger.Write("Executable located in Downloads folder. Prompting for pseudo-install...");

        ContentDialogService contentDialogService = new();
        contentDialogService.SetDialogHost(RootContentDialog);

        var contentDialogResult = await contentDialogService.ShowAsync(
            new ContentDialog()
            {
                Title = "Install Application",
                Content = "The application is currently located in your Downloads folder.\n\nWould you like to install it and add a Start Menu shortcut?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "Never",
                IsSecondaryButtonEnabled = false,
                DialogHeight = 240,
                DialogMaxHeight = 240
            },
            CancellationToken.None
        );

        if (contentDialogResult == ContentDialogResult.Primary)
        {
            string targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spectrometer");
            Directory.CreateDirectory(targetPath);
            string newExePath = Path.Combine(targetPath, Path.GetFileName(currentExePath));

            try
            {
                File.Move(currentExePath, newExePath, overwrite: true);

                CreateShortcutInStartMenu(newExePath);

                Process.Start(new ProcessStartInfo(newExePath)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                });

                if (App.SettingsMgr?.Settings is not null)
                {
                    App.SettingsMgr.Settings.IsInstallDeclined = true; // we just moved; do not ask again
                    App.SettingsMgr.SaveSettings();
                }

                Application.Current.Shutdown(); // properly close this instance
            }
            catch (Exception ex)
            {
                Logger.WriteExc(ex);
            }
        }
        else if (App.SettingsMgr?.Settings is not null)
        {
            App.SettingsMgr.Settings.IsInstallDeclined = true;
            App.SettingsMgr.SaveSettings();

            Logger.Write("User declined pseudo-install. We won't ask again. Continuing...");
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Creates a shortcut to the application in the Start Menu Programs folder.
    /// </summary>
    /// <param name="applicationPath">The path to the application's executable.</param>
    private void CreateShortcutInStartMenu(string applicationPath)
    {
        Logger.Write("Creating Start Menu shortcut...");

        string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
        string shortcutPath = Path.Combine(startMenuPath, "Spectrometer.lnk");

        // Create the WScript.Shell COM object to interact with Windows Script Host
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
        {
            Logger.WriteWarn("Failed to create WScript.Shell COM object.");
            return;
        }

        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell == null)
        {
            Logger.WriteWarn("Failed to instantiate WScript.Shell.");
            return;
        }

        var shortcut = shell.CreateShortcut(shortcutPath);
        if (shortcut == null)
        {
            Logger.WriteWarn("Failed to create shortcut object.");
            Marshal.ReleaseComObject(shell);
            return;
        }

        shortcut.TargetPath = applicationPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(applicationPath);
        shortcut.WindowStyle = 1;  // Normal window
        shortcut.Description = "Shortcut for Spectrometer application";
        shortcut.Save();

        Logger.Write("Start Menu shortcut created");

        // Cleanup
        Marshal.ReleaseComObject(shortcut);
        Marshal.ReleaseComObject(shell);
    }

    // ------------------------------------------------------------------------------------------------
    // INavigationWindow methods
    // ------------------------------------------------------------------------------------------------

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(IPageService pageService) => RootNavigation.SetPageService(pageService);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    // ------------------------------------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------------------------------------

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// Raises the closed event.
    /// Closing this window needs to begin the process of shutting down the application.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Logger.Write("Main window closed; Spectrometer shutting down...");

        Application.Current.Shutdown();
    }

    INavigationView INavigationWindow.GetNavigation()
    {
        throw new NotImplementedException();
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
}
