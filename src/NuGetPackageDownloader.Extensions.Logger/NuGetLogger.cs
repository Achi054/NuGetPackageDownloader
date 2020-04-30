using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using NugetLogger = NuGet.Common;

namespace NuGetPackageDownloader.Logging
{
    public class NuGetLogger : NugetLogger.ILogger
    {
        private readonly ILogger _logger;

        public NuGetLogger(ILogger logger) => _logger = logger;

        public void Log(NugetLogger.LogLevel level, string data)
        {
            Action<string> logMethod = level switch
            {
                NugetLogger.LogLevel.Debug => LogDebug,
                NugetLogger.LogLevel.Verbose => LogVerbose,
                NugetLogger.LogLevel.Information => LogInformation,
                NugetLogger.LogLevel.Minimal => LogMinimal,
                NugetLogger.LogLevel.Warning => LogWarning,
                NugetLogger.LogLevel.Error => LogError,
                _ => throw new ArgumentException(nameof(level))
            };
            logMethod(data);
        }

        public void Log(NugetLogger.ILogMessage message)
            => Log(message.Level, $"ErrorCode: {message.Code}\nMessage: {message.Message}");

        public Task LogAsync(NugetLogger.LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public Task LogAsync(NugetLogger.ILogMessage message)
        {
            Log(message.Level, $"ErrorCode: {message.Code}\nMessage: {message.Message}");
            return Task.CompletedTask;
        }

        public void LogDebug(string data)
            => _logger.LogDebug(data);

        public void LogError(string data)
            => _logger.LogError(data);

        public void LogInformation(string data)
            => _logger.LogInformation(data);

        public void LogInformationSummary(string data)
            => _logger.LogTrace(data);

        public void LogMinimal(string data)
            => _logger.LogInformation(data);

        public void LogVerbose(string data)
            => _logger.LogTrace(data);

        public void LogWarning(string data)
            => _logger.LogWarning(data);
    }
}
