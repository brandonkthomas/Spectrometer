using Microsoft.Extensions.Hosting;
using Spectrometer.Models;
using Spectrometer.Views.Pages;
using Spectrometer.Views.Windows;
using System.Security.RightsManagement;
using Wpf.Ui;

namespace Spectrometer.Services;

/// <summary>
/// Managed host of the application.
/// </summary>
public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    private INavigationWindow? _navigationWindow;

    private Type _pageType;

    // -------------------------------------------------------------------------------------------
    // Constructor + Start/Stop
    // -------------------------------------------------------------------------------------------

    public ApplicationHostService(IServiceProvider serviceProvider)
    {
        Logger.Write("ApplicationHostService initializing...");

        _pageType = App.SettingsMgr?.GetStartingTab() ?? typeof(DashboardPage); // get starting tab from AppSettings

        _serviceProvider = serviceProvider;

        Logger.Write("ApplicationHostService initialized");
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HandleActivationAsync();
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------------------------
    // Activation
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Creates main window during activation.
    /// </summary>
    private async Task HandleActivationAsync()
    {
        if (!Application.Current.Windows.OfType<MainWindow>().Any())
        {
            _navigationWindow = (
                _serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow
            )!;

            if (_navigationWindow is not null)
            {
                _navigationWindow!.ShowWindow();
                _navigationWindow.Navigate(_pageType);
            }
        }

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------------------------
    // Navigation Change
    // -------------------------------------------------------------------------------------------

    public void Navigate(Type pageType)
    {
        _navigationWindow?.Navigate(pageType);
    }
}
