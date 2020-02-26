using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using NugetLogger = NuGet.Common;

namespace NugetPackageDownloader.Logging
{
    public class NuGetLogger : NugetLogger.ILogger
    {
        private readonly ILogger _logger;

        public NuGetLogger(ILogger<NuGetDownloader> logger) => _logger = logger;

        public void Log(NugetLogger.LogLevel level, string data)
        {
            switch (level)
            {
                case NugetLogger.LogLevel.Debug:
                    LogDebug(data);
                    break;
                case NugetLogger.LogLevel.Verbose:
                    LogVerbose(data);
                    break;
                case NugetLogger.LogLevel.Information:
                    LogInformation(data);
                    break;
                case NugetLogger.LogLevel.Minimal:
                    LogMinimal(data);
                    break;
                case NugetLogger.LogLevel.Warning:
                    LogWarning(data);
                    break;
                case NugetLogger.LogLevel.Error:
                    LogError(data);
                    break;
                default:
                    throw new ArgumentException(nameof(level));
            }
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
