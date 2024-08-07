using Spectrometer.Models;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Spectrometer.Helpers;

// -------------------------------------------------------------------------------------------
/// <summary>
/// 
/// </summary>
public class AppUpdateManager
{
    private static readonly HttpClient client = new();

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<bool> IsUpdateAvailable()
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
                return false;
            }

            var currentVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion.Split('+')[0] ?? "0.0.0";

            if (latestVersion != currentVersion)
            {
                Logger.Write($"A new version is available: {latestVersion}");
                return true;
            }
            else
            {
                Logger.Write("The application is up to date.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
            return false;
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Downloads the latest version executable from GitHub releases.
    /// </summary>
    public async Task DownloadAndInstallLatestVersion()
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
    private static async Task DownloadFileAsync(string url, string localPath)
    {
        using HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using FileStream fs = new(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs);
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Replaces the current executable with the downloaded one and restarts the application.
    /// </summary>
    /// <param name="newExePath">The path to the downloaded executable.</param>
    private static void ReplaceAndRestart(string tempPath)
    {
        string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        string backupPath = currentExePath + ".bak";

        Logger.Write($"Current EXE path: {currentExePath}");
        Logger.Write($"Backup path: {backupPath}");
        Logger.Write($"Downloaded EXE temp path: {tempPath}");

        try
        {
            // -------------------------------------------------------------------------------------------
            // Backup current executable

            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Move(currentExePath, backupPath);

            // -------------------------------------------------------------------------------------------
            // Move new executable to current location

            File.Move(tempPath, currentExePath);

            // -------------------------------------------------------------------------------------------
            // Start the new executable

            Logger.Write("Current EXE replaced with new EXE; closing this instance & opening new instance...");

            Process.Start(new ProcessStartInfo(currentExePath)
            {
                UseShellExecute = true,
                Verb = "runas"
            });

            // -------------------------------------------------------------------------------------------
            // Exit the current application

            //if (File.Exists(backupPath))
            //    File.Delete(backupPath); // 2024-08-07: This doesn't seem to work ("permission denied"). TBD

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
