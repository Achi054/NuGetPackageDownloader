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
        private const string nuGetPath = "nuget";
        private const string packageSourcesSection = "packageSources";
        private const string disallowedPackageSourcesSection = "disabledPackageSources";

        public NuGetManager(ILogger logger)
        {
            Logger = logger;

            NuGetPath = $"{AppDomain.CurrentDomain.BaseDirectory}{nuGetPath}";

            NuGetSourceCacheContext = new SourceCacheContext
            {
                NoCache = true,
                DirectDownload = true,
                RefreshMemoryCache = true,
            };

            NuGetResourceProviders = new List<Lazy<INuGetResourceProvider>>();

            // Add API support for v3, include v2 support if needed
            NuGetResourceProviders.AddRange(Repository.Provider.GetCoreV3());

            // Set NuGet project
            NuGetProject = new FolderNuGetProject(NuGetPath);

            // NuGet config settings
            NuGetSettings = Settings.LoadDefaultSettings(NuGetPath, null, new MachineWideSettings());

            // Initialize NuGetPackageManager
            NuGetPackageManager = InitializeNuGetPackageManager();

            // Initialize NuGet Source Repositories
            InitializeNuGetSourceRepositories();

            // Initialize package resolution context
            NuGetResolutionContext = new ResolutionContext(
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
            NuGetDownloadContext = new PackageDownloadContext(
                NuGetResolutionContext.SourceCacheContext,
                NuGetPath,
                NuGetResolutionContext.SourceCacheContext.DirectDownload);

            // Initialize package project context
            NuGetProjectContext = new ProjectContext(Logger)
            {
                PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Files, XmlDocFileSaveMode.None, ClientPolicyContext.GetClientPolicy(NuGetSettings, Logger), Logger)
            };
        }

        public NuGetManager(Action<NuGetManager> nuGetManagerOptions)
        {
            nuGetManagerOptions?.Invoke(this);
            new NuGetManager(Logger);
        }

        public List<Lazy<INuGetResourceProvider>> NuGetResourceProviders { get; }

        public SourceCacheContext NuGetSourceCacheContext { get; }

        public ISettings NuGetSettings { get; }

        public NuGetPackageManager NuGetPackageManager { get; }

        public ResolutionContext NuGetResolutionContext { get; }

        public PackageDownloadContext NuGetDownloadContext { get; }

        public ProjectContext NuGetProjectContext { get; }

        public NuGetProject NuGetProject { get; }

        public bool IncludePrerelease { get; set; }

        public string NuGetPath { get; set; }

        public ILogger Logger { get; set; }

        public ICollection<SourceRepository> NuGetSourceRepositories { get; set; } = new HashSet<SourceRepository>();

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

        private void InitializeNuGetSourceRepositories()
        {
            var disallowedPackageSources = NuGetSettings.GetSection(disallowedPackageSourcesSection).Items.Select(x => (AddItem)x);

            foreach (SourceItem section in NuGetSettings.GetSection(packageSourcesSection).Items)
            {
                if (Uri.TryCreate(section.Value, UriKind.Absolute, out Uri uri)
                    && !uri.IsFile
                    && !disallowedPackageSources.Any(x => x.Key == section.Key))
                {
                    var sourceRepository = new SourceRepository(new PackageSource(uri.ToString()), NuGetResourceProviders);
                    if (IsNuGetSourceValid(sourceRepository))
                        NuGetSourceRepositories.Add(sourceRepository);
                }
            }
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
