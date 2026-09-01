using System.Text;

namespace LocalImageConverter.Core.Services;

public class FileLoggerService : ILoggerService
{
    private readonly string _logDir;
    private readonly string _logFile;
    private readonly object _lock = new();

    public FileLoggerService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDir = Path.Combine(localAppData, "LocalImageConverter", "Logs");
        _logFile = Path.Combine(_logDir, "app.log");

        try
        {
            if (!Directory.Exists(_logDir))
            {
                Directory.CreateDirectory(_logDir);
            }
        }
        catch
        {
            // Fallback to temp if LocalAppData is inaccessible
            _logDir = Path.Combine(Path.GetTempPath(), "LocalImageConverter", "Logs");
            _logFile = Path.Combine(_logDir, "app.log");
            Directory.CreateDirectory(_logDir);
        }
    }

    public void LogInfo(string message) => Write("INFO", message);
    public void LogWarning(string message) => Write("WARN", message);

    public void LogError(string message, Exception? exception = null)
    {
        var sb = new StringBuilder();
        sb.Append(message);
        if (exception != null)
        {
            sb.Append(" | Exception: ");
            sb.Append(exception.GetType().Name);
            sb.Append(": ");
            sb.Append(exception.Message);
            sb.Append(" | Stack: ");
            sb.Append(exception.StackTrace);
        }
        Write("ERROR", sb.ToString());
    }

    public string GetLogDirectoryPath() => _logDir;
    public string GetLogFilePath() => _logFile;

    public void ClearLogs()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_logFile))
                {
                    File.Delete(_logFile);
                }
            }
            catch
            {
                // Ignore failure to delete if locked
            }
        }
    }

    private void Write(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var line = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFile, line, Encoding.UTF8);
            }
            catch
            {
                // Logging failure should never crash the main application
            }
        }
    }
}
