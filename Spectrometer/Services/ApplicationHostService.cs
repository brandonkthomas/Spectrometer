﻿using Microsoft.Extensions.Hosting;
using Spectrometer.Helpers;
using Spectrometer.Views.Windows;
using System.Configuration;
using Wpf.Ui;

namespace Spectrometer.Services;

/// <summary>
/// Managed host of the application.
/// </summary>
public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    private INavigationWindow? _navigationWindow;

    private StartingTabPageHelper _startingTabHelper;

    private Type _pageType;

    public ApplicationHostService(IServiceProvider serviceProvider)
    {
        _startingTabHelper = new StartingTabPageHelper();
        _pageType = _startingTabHelper.GetStartingTabPageFromAppSettings();
        _serviceProvider = serviceProvider;
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
}
