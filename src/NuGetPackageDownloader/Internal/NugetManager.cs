using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace NuGetPackageDownloader.Internal
{
    internal sealed class NuGetManager
    {
        // Read from source nuget.config
        private const string packageSourcesSection = "packageSources";
        private const string disallowedPackageSourcesSection = "disabledPackageSources";

        private readonly string _outputPath;
        private readonly ILogger _logger;

        internal static async Task<NuGetManager> Create(TargetFramework targetFramework,
            string? outputPath,
            bool includePrerelease,
            bool recursive,
            IEnumerable<string>? sources,
            ILogger logger)
        {
            var manager = new NuGetManager(targetFramework, outputPath, includePrerelease, recursive, logger);
            // Initialize NuGet Source Repositories
            manager.SourceRepositories = await manager.GetSourceRepositories(sources);
            return manager;
        }

        private NuGetManager(TargetFramework targetFramework,
            string? outputPath,
            bool includePrerelease,
            bool recursive,
            ILogger logger)
        {
            // NuGet config settings
            Settings = NuGet.Configuration.Settings.LoadDefaultSettings(null, null, new MachineWideSettings());

            Framework = targetFramework.ToNuGetFramework();
            _outputPath = outputPath ?? SettingsUtility.GetGlobalPackagesFolder(Settings);
            IncludePrerelease = includePrerelease;
            Recursive = recursive;
            _logger = logger;

            SourceCacheContext = new SourceCacheContext
            {
                NoCache = true,
                DirectDownload = true,
                RefreshMemoryCache = true,
            };

            // Add API support for v3, include v2 support if needed
            ResourceProviders.AddRange(Repository.Provider.GetCoreV3());

            // Set NuGet project
            Project = new FolderNuGetProject(_outputPath);

            // Initialize NuGetPackageManager
            var packageSourceProvider = new PackageSourceProvider(Settings);
            var sourceRepositoryProvider = new SourceRepositoryProvider(packageSourceProvider, ResourceProviders);
            PackageManager = new NuGetPackageManager(sourceRepositoryProvider, Settings, _outputPath)
            {
                PackagesFolderNuGetProject = (FolderNuGetProject)Project,
            };

            // Initialize package resolution context
            ResolutionContext = new ResolutionContext(DependencyBehavior.Lowest,
                IncludePrerelease,
                false,
                VersionConstraints.None,
                new GatherCache(),
                new SourceCacheContext
                {
                    NoCache = true,
                    DirectDownload = true
                });

            // Initialize package download context
            DownloadContext = new PackageDownloadContext(ResolutionContext.SourceCacheContext,
                _outputPath,
                ResolutionContext.SourceCacheContext.DirectDownload);

            // Initialize package project context
            ProjectContext = new ProjectContext(_logger);
            ProjectContext.PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv2,
                XmlDocFileSaveMode.None,
                ClientPolicyContext.GetClientPolicy(Settings, _logger),
                _logger);
        }

        internal bool IncludePrerelease { get; }

        internal bool Recursive { get; }

        internal NuGetFramework Framework { get; }

        internal IEnumerable<SourceRepository> SourceRepositories { get; private set; } = null!;

        internal List<Lazy<INuGetResourceProvider>> ResourceProviders { get; } = new List<Lazy<INuGetResourceProvider>>();

        internal SourceCacheContext SourceCacheContext { get; }

        internal ISettings Settings { get; }

        internal NuGetPackageManager PackageManager { get; }

        internal ResolutionContext ResolutionContext { get; }

        internal PackageDownloadContext DownloadContext { get; }

        internal INuGetProjectContext ProjectContext { get; }

        internal NuGetProject Project { get; }

        internal async Task DownloadPackages(PackageIdentity identity, CancellationToken cancellationToken)
        {
            await PackageManager.InstallPackageAsync(Project,
                identity,
                ResolutionContext,
                ProjectContext,
                DownloadContext,
                SourceRepositories,
                Array.Empty<SourceRepository>(),
                cancellationToken);
        }

        private async Task<IEnumerable<SourceRepository>> GetSourceRepositories(IEnumerable<string>? sources)
        {
            var sourceRepositories = new HashSet<SourceRepository>();

            if (sources != null && sources.Any())
            {
                foreach (string nugetFeed in sources)
                {
                    if (Uri.TryCreate(nugetFeed, UriKind.Absolute, out Uri uri) && !uri.IsFile)
                    {
                        SourceRepository? sourceRepo = await GetSourceRepository(uri);
                        if (sourceRepo != null)
                            sourceRepositories.Add(sourceRepo);
                    }
                }
            }
            else
            {
                IEnumerable<AddItem>? disallowedPackageSources = Settings
                    .GetSection(disallowedPackageSourcesSection)?
                    .Items
                    .Cast<AddItem>();

                IEnumerable<SourceItem> packageSources = Settings
                    .GetSection(packageSourcesSection)
                    .Items
                    .Cast<SourceItem>();

                foreach (SourceItem packageSource in packageSources)
                {
                    if (Uri.TryCreate(packageSource.Value, UriKind.Absolute, out Uri uri)
                        && !uri.IsFile
                        && (disallowedPackageSources is null || !disallowedPackageSources.Any(x => x.Key == packageSource.Key)))
                    {
                        SourceRepository? sourceRepo = await GetSourceRepository(uri);
                        if (sourceRepo != null)
                            sourceRepositories.Add(sourceRepo);
                    }
                }
            }

            return sourceRepositories;
        }

        private async Task<SourceRepository?> GetSourceRepository(Uri feedUri)
        {
            var sourceRepository = new SourceRepository(new PackageSource(feedUri.ToString()), ResourceProviders);

            try
            {
                await sourceRepository.GetResourceAsync<PackageMetadataResource>();
                return sourceRepository;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.ToString());
                return null;
            }
        }
    }
}
