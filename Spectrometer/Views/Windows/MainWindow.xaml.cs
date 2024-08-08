using Spectrometer.Helpers;
using Spectrometer.Models;
using Spectrometer.ViewModels.Windows;
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
        // -------------------------------------------------------------------------------------------
        // Check for App Updates
        // Is the auto-update setting true, and have we not asked in the last 48 hours?

        if (App.SettingsMgr?.Settings?.AutomaticallyCheckForUpdates ?? true
            && App.SettingsMgr?.Settings?.LastUpdateDefer != null
            && (DateTime.Now - App.SettingsMgr.Settings.LastUpdateDefer.Value).TotalHours > 48)
        {
            AppUpdateManager updateManager = new();
            if (!await updateManager.IsUpdateAvailable())
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
                await updateManager.DownloadAndInstallLatestVersion();
            }
            else if (App.SettingsMgr?.Settings is not null)
            {
                App.SettingsMgr.Settings.LastUpdateDefer = DateTime.Now;
                App.SettingsMgr.SaveSettings();

                Logger.Write("User deferred update; asking again in 48 hours");
            }
        }

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
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Make sure that closing this window will begin the process of closing the application.
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
