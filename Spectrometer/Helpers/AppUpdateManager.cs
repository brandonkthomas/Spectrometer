using Spectrometer.Models;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows.Shell;

namespace Spectrometer.Helpers;

// -------------------------------------------------------------------------------------------
/// <summary>
/// 
/// </summary>
public class AppUpdateManager
{
    private static readonly HttpClient client = new HttpClient();

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task CheckForUpdates()
    {
        Logger.Write("Polling GitHub for available app updates...");

        try
        {
            string url = "https://api.github.com/repos/brandonkthomas/Spectrometer/releases/latest";
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Spectrometer/1.0)");

            string? response = await client.GetStringAsync(url);
            JsonObject? latestRelease = JsonNode.Parse(response)?.AsObject();
            string? latestVersion = latestRelease?["tag_name"]?.ToString();

            if (latestVersion == null)
            {
                Logger.WriteWarn("An unknown error occurred retrieving the latest application version.");
                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion.Split('+')[0] ?? "0.0.0";

            if (latestVersion != currentVersion)
            {
                Logger.Write($"A new version is available: {latestVersion}");

                MessageBoxResult dialogResult = MessageBox.Show($"A new version is available: {latestVersion}\n\nWould you like to install the update now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (dialogResult == MessageBoxResult.Yes)
                    await DownloadAndInstallLatestVersion();
            }
            else
            {
                Logger.Write("The application is up to date.");
            }
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Downloads the latest version executable from GitHub releases.
    /// </summary>
    private async Task DownloadAndInstallLatestVersion()
    {
        try
        {
            string url = "https://api.github.com/repos/brandonkthomas/Spectrometer/releases/latest";
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Spectrometer/1.0)");

            string? response = await client.GetStringAsync(url);
            JsonObject? latestRelease = JsonNode.Parse(response)?.AsObject();
            JsonArray? assets = latestRelease?["assets"]?.AsArray();

            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    string? downloadUrl = asset?["browser_download_url"]?.ToString();
                    string? assetName = asset?["name"]?.ToString();

                    if (downloadUrl != null 
                        && assetName != null 
                        && assetName.EndsWith(".exe"))
                    {
                        Logger.Write($"Downloading: {assetName}");
                        string tempPath = Path.Combine(Path.GetTempPath(), assetName);
                        await DownloadFileAsync(downloadUrl, tempPath);
                        Logger.Write($"Download completed: {tempPath}");

                        ReplaceAndRestart(tempPath);

                        break;
                    }
                }
            }
            else
            {
                Logger.WriteWarn("No assets found in the latest release.");
            }
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Downloads a file from the specified URL to the specified local path.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="localPath">The local path where the file should be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DownloadFileAsync(string url, string localPath)
    {
        using (HttpResponseMessage response = await client.GetAsync(url))
        {
            response.EnsureSuccessStatusCode();

            using (FileStream fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs);
            }
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Replaces the current executable with the downloaded one and restarts the application.
    /// </summary>
    /// <param name="newExePath">The path to the downloaded executable.</param>
    private void ReplaceAndRestart(string newExePath)
    {
        string currentExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Spectrometer.exe") ?? string.Empty;
        string backupPath = currentExePath + ".bak";

        try
        {
            // -------------------------------------------------------------------------------------------
            // Backup current executable

            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Move(currentExePath, backupPath);

            // -------------------------------------------------------------------------------------------
            // Move new executable to current location

            File.Move(newExePath, currentExePath);

            // -------------------------------------------------------------------------------------------
            // Start the new executable

            Process.Start(new ProcessStartInfo(currentExePath)
            {
                UseShellExecute = true,
                Verb = "runas"
            });

            // -------------------------------------------------------------------------------------------
            // Exit the current application

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
            Logger.WriteWarn("Update failed, restoring backup...");
            if (File.Exists(backupPath))
                File.Move(backupPath, currentExePath);
        }
    }
}
