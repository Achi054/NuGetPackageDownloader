using System;
using System.Xml.Linq;

using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectManagement;

namespace NuGetPackageDownloader.Internal
{
    public sealed class ProjectContext : INuGetProjectContext
    {
        private readonly ILogger _logger;

        internal ProjectContext(ILogger logger)
        {
            _logger = logger;
        }

        void INuGetProjectContext.Log(MessageLevel level, string message, params object[] args)
        {
            _logger.Log(GetLogLevel(level), message);
        }

        FileConflictAction INuGetProjectContext.ResolveFileConflict(string message)
        {
            return FileConflictAction.Ignore;
        }

        PackageExtractionContext INuGetProjectContext.PackageExtractionContext { get; set; } = null!;

        XDocument INuGetProjectContext.OriginalPackagesConfig { get; set; } = null!;

        ISourceControlManagerProvider INuGetProjectContext.SourceControlManagerProvider => null!;

        ExecutionContext INuGetProjectContext.ExecutionContext => null!;

        void INuGetProjectContext.ReportError(string message)
        {
            _logger.LogError(message);
        }

        NuGetActionType INuGetProjectContext.ActionType { get; set; }

        Guid INuGetProjectContext.OperationId { get; set; }

        private LogLevel GetLogLevel(MessageLevel level)
        {
            return level switch
            {
                MessageLevel.Debug => LogLevel.Debug,
                MessageLevel.Error => LogLevel.Error,
                MessageLevel.Info => LogLevel.Information,
                MessageLevel.Warning => LogLevel.Warning,
                _ => throw new ArgumentException(nameof(level))
            };
        }
    }
}
