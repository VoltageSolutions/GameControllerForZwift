using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.WPF.Logging
{
    public static class CustomMessageEventLogLoggerExtensions
    {
        public static ILoggingBuilder AddCustomEventLogProvider(this ILoggingBuilder builder, string source = ".NET Runtime")
        {
            builder.Services.AddSingleton<ILoggerProvider>(new CustomMessageEventLogLoggerProvider(source));
            return builder;
        }
    }
}
