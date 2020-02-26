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
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NugetPackageDownloader.Constants;
using NugetPackageDownloader.Resources.NuGet;
using NuGetCore = NuGet.Packaging.Core;

namespace NugetPackageDownloader.Resources
{
    internal class NuGetManager
    {
        // Read from source nuget.config
        private const string packageSourcesSection = "packageSources";
        private const string disallowedPackageSourcesSection = "disabledPackageSources";

        internal NuGetManager(
            TargetFramework targetFramework = TargetFramework.NETSTANDARD2_0,
            bool isDownloadAndExtract = default,
            string outputPath = default,
            bool includePrerelease = default,
            IEnumerable<string> nuGetFeeds = default,
            ILogger logger = default)
        {
            Logger = logger;

            IncludePrerelease = includePrerelease;

            IsDownloadAndExtract = isDownloadAndExtract;

            NuGetFramework = targetFramework.ToNuGetFramework();

            NuGetSourceCacheContext = new SourceCacheContext
            {
                NoCache = true,
                DirectDownload = true,
                RefreshMemoryCache = true,
            };

            NuGetResourceProviders = new List<Lazy<INuGetResourceProvider>>();

            // Add API support for v3, include v2 support if needed
            NuGetResourceProviders.AddRange(Repository.Provider.GetCoreV3());

            // NuGet config settings
            NuGetSettings = NuGetSettings ?? Settings.LoadDefaultSettings(null, null, new MachineWideSettings());

            // Set NuGet global path
            NuGetPath = outputPath ?? SettingsUtility.GetGlobalPackagesFolder(NuGetSettings);

            // Set NuGet project
            NuGetProject = NuGetProject ?? new FolderNuGetProject(NuGetPath);

            // Initialize NuGetPackageManager
            NuGetPackageManager = NuGetPackageManager ?? InitializeNuGetPackageManager();

            // Initialize NuGet Source Repositories
            NuGetSourceRepositories = NuGetSourceRepositories ?? InitializeNuGetSourceRepositories(nuGetFeeds);

            // Initialize package resolution context
            NuGetResolutionContext = NuGetResolutionContext ?? new ResolutionContext(
                DependencyBehavior.Lowest,
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
            NuGetDownloadContext = NuGetDownloadContext ?? new PackageDownloadContext(
                NuGetResolutionContext.SourceCacheContext,
                NuGetPath,
                NuGetResolutionContext.SourceCacheContext.DirectDownload);

            // Initialize package project context
            NuGetProjectContext = NuGetProjectContext ?? new ProjectContext(Logger)
            {
                PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv2, XmlDocFileSaveMode.None, ClientPolicyContext.GetClientPolicy(NuGetSettings, Logger), Logger)
            };
        }

        internal List<Lazy<INuGetResourceProvider>> NuGetResourceProviders { get; }

        internal SourceCacheContext NuGetSourceCacheContext { get; }

        internal ISettings NuGetSettings { get; }

        internal NuGetPackageManager NuGetPackageManager { get; }

        internal ResolutionContext NuGetResolutionContext { get; }

        internal PackageDownloadContext NuGetDownloadContext { get; }

        internal ProjectContext NuGetProjectContext { get; }

        internal NuGetProject NuGetProject { get; }

        internal IEnumerable<SourceRepository> NuGetSourceRepositories { get; }

        internal string NuGetPath { get; }

        internal ILogger Logger { get; }

        internal bool IncludePrerelease { get; }

        internal NuGetFramework NuGetFramework { get; }

        internal bool IsDownloadAndExtract { get; }

        /// <summary>
        /// Download NuGet packages based on Package Identity
        /// </summary>
        /// <param name="packageIdentity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal async Task DownloadPackages(
            NuGetCore.PackageIdentity packageIdentity,
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

            return new NuGetPackageManager(sourceRepositoryProvider, NuGetSettings, NuGetPath)
            {
                PackagesFolderNuGetProject = (FolderNuGetProject)NuGetProject
            };
        }

        private IEnumerable<SourceRepository> InitializeNuGetSourceRepositories(IEnumerable<string> nuGetFeeds)
        {
            var sourceRepositories = new HashSet<SourceRepository>();

            if (nuGetFeeds != null && nuGetFeeds.Any())
            {
                foreach (var nuGetFeed in nuGetFeeds)
                {
                    if (Uri.TryCreate(nuGetFeed, UriKind.Absolute, out Uri uri)
                        && !uri.IsFile)
                        sourceRepositories.Add(GetSourceRepository(uri));
                }
            }
            else
            {
                var disallowedPackageSources = NuGetSettings.GetSection(disallowedPackageSourcesSection).Items.Select(x => (AddItem)x);

                foreach (SourceItem section in NuGetSettings.GetSection(packageSourcesSection).Items)
                {
                    if (Uri.TryCreate(section.Value, UriKind.Absolute, out Uri uri)
                        && !uri.IsFile
                        && !disallowedPackageSources.Any(x => x.Key == section.Key))
                        sourceRepositories.Add(GetSourceRepository(uri));
                }
            }

            return sourceRepositories.Where(x => x != null);
        }

        private SourceRepository GetSourceRepository(Uri feedUri)
        {
            var sourceRepository = new SourceRepository(new PackageSource(feedUri.ToString()), NuGetResourceProviders);

            if (IsNuGetSourceValid(sourceRepository))
                return sourceRepository;
            return null;
        }

        private bool IsNuGetSourceValid(SourceRepository sourceRepository)
        {
            try
            {
                sourceRepository.GetResource<PackageMetadataResource>();
                return true;
            }
            catch (Exception)
            {
                Logger?.LogWarning($"Not able to access NuGet source {sourceRepository.PackageSource.Name}");
                return false;
            }
        }
    }
}
