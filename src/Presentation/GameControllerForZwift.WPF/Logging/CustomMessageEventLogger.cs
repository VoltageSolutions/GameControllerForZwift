using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GameControllerForZwift.WPF.Logging
{
    public class CustomMessageEventLogger : ILogger
    {
        #region Fields

        private readonly EventLog _eventLog;
        private readonly string _categoryName;

        // Default Event IDs for different log levels
        private const int InformationEventId = 1100;
        private const int WarningEventId = 1010;
        private const int ErrorEventId = 1000;

        private readonly Dictionary<LogLevel, EventLogEntryType> _logLevelMapping = new()
        {
            { LogLevel.Critical, EventLogEntryType.Error },
            { LogLevel.Error, EventLogEntryType.Error },
            { LogLevel.Warning, EventLogEntryType.Warning },
            { LogLevel.Information, EventLogEntryType.Information },
            { LogLevel.Debug, EventLogEntryType.Information },
            { LogLevel.Trace, EventLogEntryType.Information }
        };

        #endregion

        #region Constructor

        public CustomMessageEventLogger(EventLog eventLog, string categoryName)
        {
            _eventLog = eventLog;
            _categoryName = categoryName;
        }

        #endregion

        #region Methods

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string message = formatter(state, exception);
            var entryType = _logLevelMapping.ContainsKey(logLevel) ? _logLevelMapping[logLevel] : EventLogEntryType.Information;

            int eventIdValue = logLevel switch
            {
                LogLevel.Critical => ErrorEventId,
                LogLevel.Error => ErrorEventId,
                LogLevel.Warning => WarningEventId,
                _ => InformationEventId
            };

            if (entryType == EventLogEntryType.Error)
            {
                string customMessage = $"Class: {_categoryName}\n{message}";
                _eventLog.WriteEntry(customMessage, entryType, ErrorEventId);
            }
            else
            {
                _eventLog.WriteEntry(message, entryType, eventIdValue);
            }
        }

        #endregion
    }
}
