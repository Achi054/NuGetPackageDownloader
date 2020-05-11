using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

using NuGetPackageDownloader.Internal;

namespace NuGetPackageDownloader
{
    public partial class NuGetDownloader : NuGetAction
    {
        private readonly NuGetFramework _framework;
        private readonly FolderNuGetProject _project;
        private readonly NuGetPackageManager _packageManager;

        private readonly string _downloadDir;
        private readonly string? _extractDir;

        public NuGetDownloader(TargetFramework targetFramework, string? outputDir = null,
            bool includePrerelease = false, bool recursive = false, bool extract = true,
            IEnumerable<string>? sources = null)
            : base(sources)
        {
            TargetFramework = targetFramework;
            Recursive = recursive;
            Extract = extract;

            IncludePrerelease = includePrerelease;

            outputDir ??= Directory.GetCurrentDirectory();
            (_downloadDir, _extractDir) = extract
                ? (Path.Combine(Path.GetTempPath(), $"NuGetPackageDownloader.{Guid.NewGuid():N}"), outputDir)
                : (outputDir, null);

            _framework = targetFramework.ToNuGetFramework();
            _project = new FolderNuGetProject(_downloadDir);

            var packageSourceProvider = new PackageSourceProvider(Settings);
            var sourceRepositoryProvider = new SourceRepositoryProvider(packageSourceProvider, ResourceProviders);
            _packageManager = new NuGetPackageManager(sourceRepositoryProvider, Settings, _downloadDir)
            {
                PackagesFolderNuGetProject = _project,
            };
        }

        public TargetFramework TargetFramework { get; }

        public bool Recursive { get; }

        public bool Extract { get; }

        public async Task<IEnumerable<NuGetPackage>> GetPackageDependenciesAsync(string name,
            string? version = null,
            CancellationToken cancellationToken = default)
        {
            var dependencies = (await GetPackageIdentitiesAsync(name, version, cancellationToken))
                .Where(pi => pi.Id != name)
                .Select(pi => new NuGetPackage(pi.Id, pi.Version.ToNormalizedString()));
            return dependencies;
        }
    }

    public partial class NuGetDownloader
    {
        private async Task<IReadOnlyList<PackageIdentity>> GetPackageIdentitiesAsync(string name,
            string? version,
            CancellationToken cancellationToken)
        {
            var metadatas = new PackageMetadataCollection();

            IPackageSearchMetadata? metadata = await GetPackageMetadataAsync(name, version, cancellationToken);
            if (metadata is null)
                return Array.Empty<PackageIdentity>();

            metadatas.Add(metadata);

            if (Recursive)
            {
                int index = 0;
                while (index < metadatas.Count)
                {
                    IPackageSearchMetadata metadataAtIndex = metadatas[index];
                    IEnumerable<PackageIdentity> dependentPackages = GetDependentPackageIdentities(metadataAtIndex, _framework);
                    foreach (PackageIdentity dependentPackage in dependentPackages)
                    {
                        IPackageSearchMetadata? dependentMetadata = await GetPackageMetadataAsync(dependentPackage.Id,
                            dependentPackage.Version.ToNormalizedString(),
                            cancellationToken);
                        if (dependentMetadata != null)
                            metadatas.Add(dependentMetadata);
                    }

                    index++;
                }
            }

            return metadatas.Select(m => m.Identity).ToList();
        }

        private async Task<IPackageSearchMetadata?> GetPackageMetadataAsync(string name,
            string? version,
            CancellationToken cancellationToken)
        {
            foreach (SourceRepository sourceRepository in SourceRepositories)
            {
                IPackageSearchMetadata packageMetadata = await GetPackageMetadataAsync(name,
                    version, sourceRepository, cancellationToken);
                if (packageMetadata != null)
                    return packageMetadata;
            }

            return default;
        }

        private async Task<IPackageSearchMetadata> GetPackageMetadataAsync(string name,
            string? version,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            PackageMetadataResource packageMetadataResource = await sourceRepository
                .GetResourceAsync<PackageMetadataResource>();

            if (NuGetVersion.TryParse(version, out NuGetVersion nugetVersion))
            {
                // If version is specified, get the metadata for the specific version of the package.
                return await packageMetadataResource.GetMetadataAsync(
                    new PackageIdentity(name, nugetVersion),
                    SourceCacheContext,
                    Logger,
                    cancellationToken);
            }

            // If version is not specified, get metadata for all packages and return the latest.
            IEnumerable<IPackageSearchMetadata> packageMetadatas = await packageMetadataResource.GetMetadataAsync(
                name,
                IncludePrerelease,
                includeUnlisted: false,
                SourceCacheContext,
                Logger,
                cancellationToken);

            return packageMetadatas
                .OrderByDescending(x => x.Identity.Version)
                .FirstOrDefault();
        }

        private IEnumerable<PackageIdentity> GetDependentPackageIdentities(IPackageSearchMetadata packageMetadata,
            NuGetFramework targetFramework)
        {
            if (packageMetadata.DependencySets is null || !packageMetadata.DependencySets.Any())
                return Enumerable.Empty<PackageIdentity>();

            NuGetFramework mostCompatibleFramework = GetMostCompatibleFramework(targetFramework,
                packageMetadata.DependencySets);

            PackageDependencyGroup dependentPackages = packageMetadata.DependencySets
                .Where(x => x.TargetFramework == mostCompatibleFramework)
                .FirstOrDefault();

            return dependentPackages is null
                ? Enumerable.Empty<PackageIdentity>()
                : dependentPackages.Packages.Select(p => new PackageIdentity(p.Id, p.VersionRange.MinVersion));
        }

        private NuGetFramework GetMostCompatibleFramework(NuGetFramework projectTargetFramework,
            IEnumerable<PackageDependencyGroup> itemGroups)
        {
            return new FrameworkReducer().GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));
        }
    }

    public partial class NuGetDownloader
    {
        public async Task DownloadPackageAsync(string packageName, string? version = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PackageIdentity> packageIdentities = await GetPackageIdentitiesAsync(packageName, version,
                cancellationToken);

            IEnumerable<Task> tasks = packageIdentities.Select(async identity =>
            {
                bool packageAlreadyExists = _packageManager.PackageExistsInPackagesFolder(identity,
                    PackageSaveMode.None);

                if (!packageAlreadyExists)
                    await DownloadPackages(identity, cancellationToken);
            });
            await Task.WhenAll(tasks);

            if (Extract)
            {
                try
                {
                    ExtractPackageAssemblies(packageIdentities);
                }
                finally
                {
                    try
                    {
                        Directory.Delete(_downloadDir, true);
                    }
                    catch
                    {
                        // Swallow exception
                    }
                }
            }
        }

        private async Task DownloadPackages(PackageIdentity identity, CancellationToken cancellationToken)
        {
            var resolutionContext = new ResolutionContext(DependencyBehavior.Lowest,
                IncludePrerelease,
                false,
                VersionConstraints.None,
                new GatherCache(),
                new SourceCacheContext
                {
                    NoCache = true,
                    DirectDownload = true
                });

            INuGetProjectContext projectContext = new ProjectContext(Logger);
            projectContext.PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv2,
                XmlDocFileSaveMode.None,
                ClientPolicyContext.GetClientPolicy(Settings, Logger),
                Logger);

            var downloadContext = new PackageDownloadContext(resolutionContext.SourceCacheContext, _downloadDir,
                resolutionContext.SourceCacheContext.DirectDownload);

            await _packageManager.InstallPackageAsync(_project,
                identity,
                resolutionContext,
                projectContext,
                downloadContext,
                SourceRepositories,
                Array.Empty<SourceRepository>(),
                cancellationToken);
        }

        private void ExtractPackageAssemblies(IEnumerable<PackageIdentity> identities)
        {
            if (!Extract)
                return;

            var project = new FolderNuGetProject(_downloadDir);

            if (!Directory.Exists(_extractDir))
                Directory.CreateDirectory(_extractDir);

            foreach (PackageIdentity identity in identities)
            {
                string packageFilePath = project.GetInstalledPackageFilePath(identity);
                if (string.IsNullOrWhiteSpace(packageFilePath))
                    continue;

                FrameworkSpecificGroup? referenceGroup;
                using (var archiveReader = new PackageArchiveReader(packageFilePath, null, null))
                    referenceGroup = GetMostCompatibleGroup(_framework, archiveReader.GetReferenceItems());

                if (referenceGroup is null || referenceGroup.Items is null || !referenceGroup.Items.Any())
                    continue;

                string nugetPackagePath = project.GetInstalledPath(identity);
                Parallel.ForEach(referenceGroup.Items, x =>
                {
                    string sourceAssemblyPath = Path.Combine(nugetPackagePath, x);

                    string assemblyName = Path.GetFileName(sourceAssemblyPath);
                    string destinationAssemblyPath = Path.Combine(_extractDir, assemblyName);

                    File.Copy(sourceAssemblyPath, destinationAssemblyPath, true);
                });
            }
        }

        private FrameworkSpecificGroup? GetMostCompatibleGroup(NuGetFramework projectTargetFramework,
            IEnumerable<FrameworkSpecificGroup> itemGroups)
        {
            NuGetFramework mostCompatibleFramework = new FrameworkReducer()
                .GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));

            if (mostCompatibleFramework is null)
                return null;

            FrameworkSpecificGroup mostCompatibleGroup = itemGroups.FirstOrDefault(i => i.TargetFramework.Equals(mostCompatibleFramework));
            return IsValid(mostCompatibleGroup) ? mostCompatibleGroup : null;
        }

        private bool IsValid(FrameworkSpecificGroup frameworkSpecificGroup)
        {
            if (frameworkSpecificGroup is null)
                return false;

            return frameworkSpecificGroup.HasEmptyFolder
                || frameworkSpecificGroup.Items.Any()
                || !frameworkSpecificGroup.TargetFramework.Equals(NuGetFramework.AnyFramework);
        }
    }

    public sealed class NuGetPackage
    {
        internal NuGetPackage(string id, string version)
        {
            Id = id;
            Version = version;
        }

        public string Id { get; set; }

        public string Version { get; set; }
    }
}
