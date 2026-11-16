namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Thrown when a configuration file cannot be read. The message is written for the user.
    /// </summary>
    public class ConfigurationFormatException : Exception
    {
        /// <summary>
        /// Gets a value indicating whether the file was written by a newer version of the app (CFG-007).
        /// </summary>
        public bool IsNewerFormat { get; }

        public ConfigurationFormatException()
        {
        }

        public ConfigurationFormatException(string message) : base(message)
        {
        }

        public ConfigurationFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ConfigurationFormatException(string message, bool isNewerFormat) : base(message)
        {
            IsNewerFormat = isNewerFormat;
        }
    }
}
