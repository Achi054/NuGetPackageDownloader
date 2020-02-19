using System;
using System.Xml.Linq;

using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectManagement;

namespace NugetPackageDownloader.Resources.NuGet
{
    public class ProjectContext : INuGetProjectContext
    {
        private readonly ILogger _logger;

        public ProjectContext(ILogger logger) => _logger = logger;

        public void Log(MessageLevel level, string message, params object[] args)
            => _logger.Log(GetLogLevel(level), message);

        public FileConflictAction ResolveFileConflict(string message) => FileConflictAction.Ignore;

        public PackageExtractionContext PackageExtractionContext { get; set; }

        public XDocument OriginalPackagesConfig { get; set; }

        public ISourceControlManagerProvider SourceControlManagerProvider => null;

        public ExecutionContext ExecutionContext => null;

        public void ReportError(string message) => _logger.LogError(message);

        public NuGetActionType ActionType { get; set; }

        public Guid OperationId { get; set; }

        private LogLevel GetLogLevel(MessageLevel level)
            => level switch
            {
                MessageLevel.Debug => LogLevel.Debug,
                MessageLevel.Error => LogLevel.Error,
                MessageLevel.Info => LogLevel.Information,
                MessageLevel.Warning => LogLevel.Warning,
                _ => throw new ArgumentException(nameof(level))
            };
    }
}
