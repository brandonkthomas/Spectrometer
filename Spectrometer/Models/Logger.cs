using Spectrometer.Services;
using System.Runtime.CompilerServices;

namespace Spectrometer.Models;

public static class Logger
{
    private static readonly LoggingService _loggingService;

    static Logger() => _loggingService = new LoggingService();

    public static void Write(string message,
                             [CallerMemberName] string memberName = "",
                             [CallerFilePath] string filePath = "",
                             [CallerLineNumber] int lineNumber = 0)
    {
        _loggingService.Write(message, memberName, filePath, lineNumber);
    }

    public static void WriteWarn(string message,
                                 [CallerMemberName] string memberName = "",
                                 [CallerFilePath] string filePath = "",
                                 [CallerLineNumber] int lineNumber = 0)
    {
        _loggingService.WriteWarn(message, memberName, filePath, lineNumber);
    }

    public static void WriteExc(Exception exception,
                                [CallerMemberName] string memberName = "",
                                [CallerFilePath] string filePath = "",
                                [CallerLineNumber] int lineNumber = 0)
    {
        _loggingService.WriteExc(exception, memberName, filePath, lineNumber);
    }

    public static void Dispose() => _loggingService.Dispose();
}
