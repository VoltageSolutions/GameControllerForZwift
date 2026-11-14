using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.WPF.Logging;

/// <summary>Minimal thread-safe file logger: one timestamped line per entry, flushed immediately.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly object _gate = new();
    private readonly StreamWriter _writer;

    /// <summary>Full path of the log file being written.</summary>
    public string FilePath { get; }

    /// <summary>Opens (creating directories as needed) <paramref name="filePath"/> for append.</summary>
    public FileLoggerProvider(string filePath)
    {
        FilePath = filePath;
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, ShortName(categoryName));

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            try { _writer.Dispose(); } catch (IOException) { }
        }
    }

    private static string ShortName(string category)
    {
        int i = category.LastIndexOf('.');
        return i >= 0 && i < category.Length - 1 ? category[(i + 1)..] : category;
    }

    private void Write(string line)
    {
        lock (_gate)
        {
            try { _writer.WriteLine(line); }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException) { }
        }
    }

    private static string LevelText(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "???",
    };

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _owner;
        private readonly string _category;

        public FileLogger(FileLoggerProvider owner, string category)
        {
            _owner = owner;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff"))
              .Append(' ').Append(LevelText(logLevel))
              .Append(' ').Append(_category)
              .Append(": ").Append(formatter(state, exception));
            if (exception is not null)
                sb.Append(Environment.NewLine).Append(exception);
            _owner.Write(sb.ToString());
        }
    }
}

/// <summary>Registration helpers for <see cref="FileLoggerProvider"/>.</summary>
public static class FileLoggerExtensions
{
    /// <summary>Adds a file logger writing <c>app-yyyyMMdd-HHmmss.log</c> into <paramref name="directory"/>.</summary>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string directory)
    {
        var path = Path.Combine(directory, $"app-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        builder.Services.AddSingleton<ILoggerProvider>(new FileLoggerProvider(path));
        return builder;
    }
}
