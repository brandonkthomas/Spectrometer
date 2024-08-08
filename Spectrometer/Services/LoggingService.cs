using System.Diagnostics;
using System.IO;

namespace Spectrometer.Services;

// -------------------------------------------------------------------------------------------
/// <summary>
/// Logging service that handles writing logs to a file in appdata/roaming.
/// </summary>
public class LoggingService : IDisposable
{
    private readonly string _logDirectory = string.Empty;
    private readonly string _logFilePath = string.Empty;
    private readonly StreamWriter? _streamWriter;

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Opens Roaming\Spectrometer, handles existing log files, creates a new log file & opens a StreamWriter.
    /// </summary>
    public LoggingService()
    {
        try
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spectrometer\\Logs");

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

            this.Write("Logging service started", "Logger", "Logger", 40);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Debug.WriteLine(ex);
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logFilePath"></param>
    private void HandleExistingLogFiles(string logFilePath)
    {
        // -------------------------------------------------------------------------------------------
        // Rename existing log files if needed

        if (File.Exists(logFilePath))
        {
            int modifiedLogs = 0;

            for (int i = 0; ; i++)
            {
                string newFilePath = Path.Combine(_logDirectory, $"Spectrometer_{DateTime.Now:yyyy-MM-dd}_{i:D3}.log");
                if (!File.Exists(newFilePath))
                {
                    File.Move(logFilePath, newFilePath);
                    modifiedLogs += 1;
                    break;
                }
            }

            this.Write($"{modifiedLogs} existing log file(s) renamed", "Logger", "Logger", 74);
        }

        // -------------------------------------------------------------------------------------------
        // Delete any log files older than 7 days

        int deletedLogs = 0;
        string[] logFiles = Directory.GetFiles(_logDirectory, "Spectrometer_*.log");

        foreach (string logFile in logFiles)
        {
            FileInfo fileInfo = new FileInfo(logFile);

            if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-7))
                File.Delete(logFile);
        }

        if (deletedLogs > 0)
            this.Write($"{deletedLogs} log file(s) older than 7 days deleted", "Logger", "Logger", 92);
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public void Write(string message, string memberName, string filePath, int lineNumber)
    {
        string logMessage = FormatLogMessage("", message, memberName, filePath, lineNumber);
        _streamWriter?.WriteLine(logMessage);
        Debug.WriteLine(logMessage);
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public void WriteWarn(string message, string memberName, string filePath, int lineNumber)
    {
        string logMessage = FormatLogMessage("WARN", message, memberName, filePath, lineNumber);
        _streamWriter?.WriteLine(logMessage);
        Debug.WriteLine(logMessage);
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ex"></param>
    public void WriteExc(Exception ex, string memberName, string filePath, int lineNumber)
    {
        string logMessage = FormatLogMessage("ERROR", $"{ex.Message}{Environment.NewLine}{ex.StackTrace}", memberName, filePath, lineNumber);
        _streamWriter?.WriteLine(logMessage);
        Debug.WriteLine(logMessage);
    }

    private string FormatLogMessage(string level, string message, string memberName, string filePath, int lineNumber)
    {
        return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{(level == "" ? "" : $" [{level}]")} - {Path.GetFileName(filePath)}.{memberName}:{lineNumber} - {message}";
    }

    public void Dispose() => _streamWriter?.Dispose();
}
