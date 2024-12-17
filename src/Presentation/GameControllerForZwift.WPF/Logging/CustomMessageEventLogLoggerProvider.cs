using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GameControllerForZwift.WPF.Logging
{
    /// <summary>
    /// Enables writing to the Application Windows Event Log using the source .NET Runtime with customized event messages.
    /// </summary>
    public class CustomMessageEventLogLoggerProvider : ILoggerProvider
    {
        #region Fields
        private readonly string _source;
        private readonly EventLog _eventLog;
        #endregion

        #region Constructor
        public CustomMessageEventLogLoggerProvider(string source = ".NET Runtime")
        {
            _source = source;
            _eventLog = new EventLog("Application") { Source = _source };
        }
        #endregion

        #region Methods

        public ILogger CreateLogger(string categoryName)
        {
            return new CustomMessageEventLogger(_eventLog, categoryName);
        }

        public void Dispose()
        {
            _eventLog?.Dispose();
        }
        #endregion
    }
}
