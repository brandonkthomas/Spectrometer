using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectrometer.Helpers;
using Spectrometer.Models;
using Spectrometer.Services;
using Spectrometer.ViewModels.Pages;
using Spectrometer.ViewModels.UserControls;
using Spectrometer.ViewModels.Windows;
using Spectrometer.Views.Pages;
using Spectrometer.Views.UserControls;
using Spectrometer.Views.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace Spectrometer;

// -------------------------------------------------------------------------------------------
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // ------------------------------------------------------------------------------------------------
    // Global Fields
    // ------------------------------------------------------------------------------------------------

    public static AppSettingsManager? SettingsMgr { get; private set; }

    // ------------------------------------------------------------------------------------------------
    // Entry + Configuration
    // ------------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Application primary entry point.
    /// This should contain *only* crucial processes.
    /// </summary>
    static App()
    {
        App.SettingsMgr = new(); // load AppSettings first before any other code fires
        Logger.Write("Spectrometer starting...");

        // Set custom accent color (Not working currently!)
        //ApplicationAccentColorManager.Apply(Color.FromArgb(0, 27, 170, 76));
    }

    // -------------------------------------------------------------------------------------------
    // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) ?? ""); })
        .ConfigureServices((context, services) =>
        {
            services.AddHostedService<ApplicationHostService>();

            // Page resolver service
            services.AddSingleton<IPageService, PageService>();

            // Theme manipulation
            services.AddSingleton<IThemeService, ThemeService>();

            // TaskBar manipulation
            services.AddSingleton<ITaskBarService, TaskBarService>();

            // Hardware monitor service (singleton so that only one instance is active across the application)
            services.AddSingleton<HardwareMonitorService>();

            // Process info service
            //services.AddSingleton<ProcessesService>();

            // Logging service
            services.AddSingleton<LoggingService>();

            // Service containing navigation, same as INavigationWindow... but without window
            services.AddSingleton<INavigationService, NavigationService>();

            // Main window with navigation
            services.AddSingleton<INavigationWindow, MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            // Register ViewModels with MainWindowViewModel dependency
            services.AddSingleton(provider => new DashboardViewModel(provider.GetRequiredService<MainWindowViewModel>()));
            services.AddSingleton(provider => new SensorsViewModel(provider.GetRequiredService<MainWindowViewModel>()));
            services.AddSingleton(provider => new GraphsViewModel(provider.GetRequiredService<MainWindowViewModel>()));

            // Register Pages
            services.AddSingleton<DashboardPage>();
            services.AddSingleton<SensorsPage>();
            services.AddSingleton(provider => new GraphsPage(provider.GetRequiredService<GraphsViewModel>(), provider.GetRequiredService<MainWindowViewModel>()));

            services.AddSingleton<GraphUserControl>();
            services.AddTransient(provider => new GraphViewModel(provider.GetRequiredService<MainWindowViewModel>(), provider.GetRequiredService<HardwareSensor>()));

            services.AddSingleton<SettingsPage>();
            services.AddSingleton<SettingsViewModel>();
        }).Build();

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Gets registered service.
    /// </summary>
    /// <typeparam name="T">Type of the service to get.</typeparam>
    /// <returns>Instance of the service or <see langword="null"/>.</returns>
    public static T? GetService<T>()
        where T : class
    {
        return _host.Services.GetService(typeof(T)) as T ?? null;
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => LogUnhandledException(args.ExceptionObject as Exception);

        _host.Start();
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    private async void OnExit(object sender, ExitEventArgs e)
    {
        GetService<HardwareMonitorService>()?.Dispose();
        GetService<LoggingService>()?.Dispose();
        //_host.Services.GetService<LoggingService>()?.Dispose();

        await _host.StopAsync();
        _host.Dispose();
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ex"></param>
    private void LogUnhandledException(Exception? ex)
    {
        if (ex == null) return;

        try
        {
            Logger.WriteExc(ex);
        }
        catch (Exception ex2)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {ex2.Message}{Environment.NewLine}{ex2.StackTrace}{Environment.NewLine}");
        }

        MessageBox.Show("An internal error occurred. The application could not recover and will now close. Please check the log file for more information.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        Environment.Exit(1);
    }
}
