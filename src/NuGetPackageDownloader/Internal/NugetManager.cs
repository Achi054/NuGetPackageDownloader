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
    internal class NuGetManager
    {
        // Read from source nuget.config
        private const string packageSourcesSection = "packageSources";
        private const string disallowedPackageSourcesSection = "disabledPackageSources";

        private readonly string _nugetConfigPath;
        private readonly bool _includePrerelease;
        private readonly ILogger _logger;

        internal static async Task<NuGetManager> Create(TargetFramework targetFramework,
            string? nugetConfigPath,
            bool includePrerelease,
            IEnumerable<string>? sources,
            ILogger logger)
        {
            var manager = new NuGetManager(targetFramework, nugetConfigPath, includePrerelease, logger);
            // Initialize NuGet Source Repositories
            manager.NuGetSourceRepositories = await manager.GetSourceRepositories(sources);
            return manager;
        }

        private NuGetManager(TargetFramework targetFramework,
            string? nugetConfigPath,
            bool includePrerelease,
            ILogger logger)
        {
            // NuGet config settings
            NuGetSettings = Settings.LoadDefaultSettings(null, null, new MachineWideSettings());

            NuGetFramework = targetFramework.ToNuGetFramework();
            _nugetConfigPath = nugetConfigPath ?? SettingsUtility.GetGlobalPackagesFolder(NuGetSettings);
            _includePrerelease = includePrerelease;
            _logger = logger;

            NuGetSourceCacheContext = new SourceCacheContext
            {
                NoCache = true,
                DirectDownload = true,
                RefreshMemoryCache = true,
            };

            // Add API support for v3, include v2 support if needed
            NuGetResourceProviders.AddRange(Repository.Provider.GetCoreV3());

            // Set NuGet project
            NuGetProject = new FolderNuGetProject(_nugetConfigPath);

            // Initialize NuGetPackageManager
            NuGetPackageManager = InitializeNuGetPackageManager();

            // Initialize package resolution context
            NuGetResolutionContext = new ResolutionContext(
                DependencyBehavior.Lowest,
                _includePrerelease,
                false,
                VersionConstraints.None,
                new GatherCache(),
                new SourceCacheContext
                {
                    NoCache = true,
                    DirectDownload = true
                });

            // Initialize package download context
            NuGetDownloadContext = new PackageDownloadContext(
                NuGetResolutionContext.SourceCacheContext,
                _nugetConfigPath,
                NuGetResolutionContext.SourceCacheContext.DirectDownload);

            // Initialize package project context
            NuGetProjectContext = new ProjectContext(_logger);
            NuGetProjectContext.PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv2,
                XmlDocFileSaveMode.None,
                ClientPolicyContext.GetClientPolicy(NuGetSettings, _logger),
                _logger);
        }

        internal NuGetFramework NuGetFramework { get; }

        internal IEnumerable<SourceRepository> NuGetSourceRepositories { get; private set; } = null!;

        internal List<Lazy<INuGetResourceProvider>> NuGetResourceProviders { get; } = new List<Lazy<INuGetResourceProvider>>();

        internal SourceCacheContext NuGetSourceCacheContext { get; }

        internal ISettings NuGetSettings { get; }

        internal NuGetPackageManager NuGetPackageManager { get; }

        internal ResolutionContext NuGetResolutionContext { get; }

        internal PackageDownloadContext NuGetDownloadContext { get; }

        internal INuGetProjectContext NuGetProjectContext { get; }

        internal NuGetProject NuGetProject { get; }

        /// <summary>
        /// Download NuGet packages based on Package Identity
        /// </summary>
        /// <param name="packageIdentity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal async Task DownloadPackages(
            PackageIdentity packageIdentity,
            CancellationToken cancellationToken = default)
        {
            await NuGetPackageManager.InstallPackageAsync(
                                       NuGetProject,
                                       packageIdentity,
                                       NuGetResolutionContext,
                                       NuGetProjectContext,
                                       NuGetDownloadContext,
                                       NuGetSourceRepositories,
                                       Array.Empty<SourceRepository>(),
                                       cancellationToken);
        }

        private NuGetPackageManager InitializeNuGetPackageManager()
        {
            var packageSourceProvider = new PackageSourceProvider(NuGetSettings);
            var sourceRepositoryProvider = new SourceRepositoryProvider(packageSourceProvider, NuGetResourceProviders);

            return new NuGetPackageManager(sourceRepositoryProvider, NuGetSettings, _nugetConfigPath)
            {
                PackagesFolderNuGetProject = (FolderNuGetProject)NuGetProject
            };
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
                IEnumerable<AddItem>? disallowedPackageSources = NuGetSettings
                    .GetSection(disallowedPackageSourcesSection)?
                    .Items
                    .Cast<AddItem>();

                IEnumerable<SourceItem> packageSources = NuGetSettings
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
            var sourceRepository = new SourceRepository(new PackageSource(feedUri.ToString()), NuGetResourceProviders);

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
