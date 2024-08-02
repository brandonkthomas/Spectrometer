using Spectrometer.Services;

namespace Spectrometer.Models;

public static class Logger
{
    private static readonly LoggingService _loggingService;

    static Logger() => _loggingService = new LoggingService();

    public static void Write(string message) => _loggingService.Write(message);

    public static void WriteWarn(string message) => _loggingService.WriteWarn(message);

    public static void WriteExc(Exception exception) => _loggingService.WriteExc(exception);

    public static void Dispose() => _loggingService.Dispose();
}
