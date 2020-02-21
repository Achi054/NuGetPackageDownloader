using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NugetPackageDownloader.Resources.NuGet;
using NuGetCore = NuGet.Packaging.Core;

namespace NugetPackageDownloader.Resources
{
    public class NuGetManager
    {
        // Read from source nuget.config
        private const string nuGetFolder = "nuget";
        private const string packageSourcesSection = "packageSources";
        private const string disallowedPackageSourcesSection = "disabledPackageSources";

        public NuGetManager(ILogger logger,
            bool includePrerelease = default,
            IEnumerable<string> nuGetFeeds = default)
        {
            Logger = logger;

            IncludePrerelease = includePrerelease;

            NuGetSourceCacheContext = new SourceCacheContext
            {
                NoCache = true,
                DirectDownload = true,
                RefreshMemoryCache = true,
            };

            NuGetResourceProviders = new List<Lazy<INuGetResourceProvider>>();

            // Add API support for v3, include v2 support if needed
            NuGetResourceProviders.AddRange(Repository.Provider.GetCoreV3());

            NuGetPath = $"{AppDomain.CurrentDomain.BaseDirectory}{nuGetFolder}";

            // Set NuGet project
            NuGetProject = NuGetProject ?? new FolderNuGetProject(NuGetPath);

            // NuGet config settings
            NuGetSettings = NuGetSettings ?? Settings.LoadDefaultSettings(NuGetPath, null, new MachineWideSettings());

            // Initialize NuGetPackageManager
            NuGetPackageManager = NuGetPackageManager ?? InitializeNuGetPackageManager();

            // Initialize NuGet Source Repositories
            NuGetSourceRepositories = NuGetSourceRepositories ?? InitializeNuGetSourceRepositories(nuGetFeeds);

            // Initialize package resolution context
            NuGetResolutionContext = NuGetResolutionContext ?? new ResolutionContext(
                DependencyBehavior.Lowest,
                includePrerelease,
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
            NuGetProjectContext = NuGetProjectContext ?? new ProjectContext(logger)
            {
                PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Files, XmlDocFileSaveMode.None, ClientPolicyContext.GetClientPolicy(NuGetSettings, logger), logger)
            };
        }

        public List<Lazy<INuGetResourceProvider>> NuGetResourceProviders { get; }

        public SourceCacheContext NuGetSourceCacheContext { get; }

        public ISettings NuGetSettings { get; }

        public NuGetPackageManager NuGetPackageManager { get; }

        public ResolutionContext NuGetResolutionContext { get; }

        public PackageDownloadContext NuGetDownloadContext { get; }

        public ProjectContext NuGetProjectContext { get; }

        public NuGetProject NuGetProject { get; }

        public IEnumerable<SourceRepository> NuGetSourceRepositories { get; }

        public string NuGetPath { get; }

        public ILogger Logger { get; }

        public bool IncludePrerelease { get; }

        /// <summary>
        /// Download NuGet packages based on Package Identity
        /// </summary>
        /// <param name="packageIdentity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DownloadPackages(
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
                Logger.LogWarning($"Not able to access NuGet source {sourceRepository.PackageSource.Name}");
                return false;
            }
        }
    }
}
