using System.Diagnostics;
using System.IO;

namespace Spectrometer.Services;

// ------------------------------------------------------------------------------------------------
/// <summary>
/// Logging service that handles writing logs to a file in appdata/roaming.
/// </summary>
public class LoggingService : IDisposable
{
    private readonly string _logDirectory = string.Empty;
    private readonly string _logFilePath = string.Empty;
    private readonly StreamWriter? _streamWriter;

    public LoggingService()
    {
        try
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spectrometer");

            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);

            string logFileName = $"Spectrometer_{DateTime.Now:yyyy-MM-dd}.log";
            _logFilePath = Path.Combine(_logDirectory, logFileName);

            // Rename existing log files if needed
            HandleExistingLogFiles(_logFilePath);

            _streamWriter = new StreamWriter(_logFilePath, append: true)
            {
                AutoFlush = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void HandleExistingLogFiles(string logFilePath)
    {
        if (File.Exists(logFilePath))
        {
            for (int i = 0; ; i++)
            {
                string newFilePath = Path.Combine(_logDirectory, $"Spectrometer_{DateTime.Now:yyyy-MM-dd}_{i:D3}.log");
                if (!File.Exists(newFilePath))
                {
                    File.Move(logFilePath, newFilePath);
                    break;
                }
            }
        }
    }

    public void Write(string message)
    {
        _streamWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}");
        Debug.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}");
    }

    public void WriteExc(Exception ex)
    {
        _streamWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - ERROR: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        Debug.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - ERROR: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
    }

    public void Dispose() => _streamWriter?.Dispose();
}
