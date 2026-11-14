using System.Text;
using Microsoft.Extensions.Logging;

namespace ZwiftLanProbe;

/// <summary>Tiny file logger for the probe (--log-file): one timestamped line per entry, appended and flushed immediately.</summary>
internal sealed class FileLogProvider : ILoggerProvider
{
    private readonly object _gate = new();
    private readonly StreamWriter _writer;

    public FileLogProvider(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false)) { AutoFlush = true };
    }

    public ILogger CreateLogger(string categoryName) => new FileLog(this, categoryName[(categoryName.LastIndexOf('.') + 1)..]);

    public void Dispose()
    {
        lock (_gate)
            _writer.Dispose();
    }

    private void Write(string line)
    {
        lock (_gate)
        {
            try { _writer.WriteLine(line); }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException) { }
        }
    }

    private sealed class FileLog(FileLogProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            var line = $"{DateTime.Now:yyyy-MM-dd'T'HH:mm:ss.fff} {logLevel.ToString()[..3].ToUpperInvariant()} {category}: {formatter(state, exception)}";
            if (exception is not null)
                line += Environment.NewLine + exception;
            owner.Write(line);
        }
    }
}
