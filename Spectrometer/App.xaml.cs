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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Wpf.Ui;

namespace Spectrometer;

// -------------------------------------------------------------------------------------------
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // -------------------------------------------------------------------------------------------
    // Global Fields
    // -------------------------------------------------------------------------------------------

    public static AppSettingsManager? SettingsMgr { get; private set; }

    // -------------------------------------------------------------------------------------------
    // Entry + Configuration
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Application primary entry point.
    /// This should contain *only* crucial processes.
    /// </summary>
    static App()
    {
        // -------------------------------------------------------------------------------------------
        // If an existing process exists, focus that one and shut this one down
        // We want to do this first so that we don't interfere with other instances' log files / settings

        Process currentProcess = Process.GetCurrentProcess();
        Process? existingProcess = Process.GetProcessesByName(currentProcess.ProcessName).FirstOrDefault(p => p.Id != currentProcess.Id);

        if (existingProcess != null)
        {
            NativeMethods.FocusWindow(existingProcess.MainWindowHandle);
            Environment.Exit(0);
            return;
        }

        // -------------------------------------------------------------------------------------------
        // Start up as normal

        App.SettingsMgr = new(); // load AppSettings first before any other code fires
        Logger.Write("Spectrometer starting...");
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
            // Application host service (HOSTED)
            services.AddHostedService<ApplicationHostService>();

            // Hardware monitor service (HOSTED)
            services.AddHostedService<HardwareMonitorService>();

            // Page resolver service
            services.AddSingleton<IPageService, PageService>();

            // Theme manipulation
            services.AddSingleton<IThemeService, ThemeService>();

            // TaskBar manipulation
            services.AddSingleton<ITaskBarService, TaskBarService>();

            // Logging service
            services.AddSingleton<LoggingService>();

            // Service containing navigation, same as INavigationWindow... but without window
            services.AddSingleton<INavigationService, NavigationService>();

            // Main window with navigation
            services.AddSingleton<MainWindowViewModel>(provider =>
            {
                return new(provider.GetServices<IHostedService>().OfType<HardwareMonitorService>().FirstOrDefault() 
                    ?? throw new Exception("An unknown error occurred loading Hardware Monitor support files."));
            });
            
            services.AddSingleton<INavigationWindow, MainWindow>();

            // Dashboard
            services.AddSingleton<DashboardViewModel>(provider => new(
                provider.GetRequiredService<MainWindowViewModel>()));

            //services.AddSingleton<DashboardPage>(provider => new(
            //    provider.GetRequiredService<DashboardViewModel>(),
            //    (ApplicationHostService)provider.GetRequiredService<IHostedService>()));

            services.AddSingleton<DashboardPage>(provider =>
            {
                DashboardViewModel? dashboardViewModel = provider.GetRequiredService<DashboardViewModel>();
                ApplicationHostService? hostService = provider.GetServices<IHostedService>().OfType<ApplicationHostService>().FirstOrDefault();
                return new(dashboardViewModel, hostService ?? throw new Exception("An unknown error occurred loading the Dashboard page."));
            });

            // Sensors
            services.AddSingleton<SensorsViewModel>(provider => new(
                provider.GetRequiredService<MainWindowViewModel>()));

            services.AddSingleton<SensorsPage>();

            // Graphs
            services.AddSingleton<GraphsViewModel>(provider => new(
                provider.GetRequiredService<MainWindowViewModel>()));

            services.AddSingleton<GraphsPage>(provider => new(
                provider.GetRequiredService<GraphsViewModel>(), 
                provider.GetRequiredService<MainWindowViewModel>()));

            // Graph UC + ViewModel
            services.AddSingleton<GraphUserControl>();
            services.AddTransient<GraphViewModel>(provider => new(
                provider.GetRequiredService<MainWindowViewModel>(), 
                provider.GetRequiredService<HardwareSensor>()));

            // Settings
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<SettingsPage>();

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
        // HardwareMonitorService is stopped by HostedService

        GetService<LoggingService>()?.Dispose();

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

// -------------------------------------------------------------------------------------------
/// <summary>
/// User32 native APIs
/// </summary>
internal static class NativeMethods
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    public static void FocusWindow(IntPtr hWnd)
    {
        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
        }
        SetForegroundWindow(hWnd);
    }
}
