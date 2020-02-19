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
using NuGet.Versioning;

using NugetPackageDownloader.Helpers;
using NugetPackageDownloader.Resources.NuGet;
using NuGetCore = NuGet.Packaging.Core;

namespace NugetPackageDownloader.Resources.Metadata
{
    public class PackageMetadata : IPackageMetadata
    {
        private readonly NugetManager _nuGetManager;
        private readonly ILogger _logger;

        private HashSet<PackageIdentity> _packageIdentities = new HashSet<PackageIdentity>();

        public PackageMetadata(ILogger logger, NugetManager nugetManager)
            => (_logger, _nuGetManager) = (logger, nugetManager);

        public async Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            TargetFramework targetFramework = TargetFramework.NETSTANDARD2_1,
            bool includePrerelease = default,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Searching package metadata for {packageName}");

                var packageSearchResource = await GetPackageMetadataResource<PackageSearchResource>();

                var searchFilter = new SearchFilter(includePrerelease, SearchFilterType.IsLatestVersion)
                {
                    SupportedFrameworks = new[] { targetFramework.ToNuGetFramework().ToString() },
                    IncludeDelisted = false,
                    OrderBy = SearchOrderBy.Id
                };

                return await packageSearchResource.SearchAsync(packageName, searchFilter, 0, 10, _logger, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<IEnumerable<PackageIdentity>> GetPackageIdentities(
            string name,
            string version,
            TargetFramework targetFramework = TargetFramework.NETSTANDARD2_1,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Fetching package identities");

            var packageIdentities = new HashSet<PackageIdentity>();

            var packageMetadata = await GetPackageIdentity(name, version, targetFramework, cancellationToken);

            if (packageMetadata != null && packageMetadata.DependentPackageIdentities.Any())
            {
                _logger.LogInformation($"Package Name: {packageMetadata.Name}\nPackage Version: {packageMetadata.Identity.Version.ToString()}\nDependencies:\n{string.Join("\n", packageMetadata.DependentPackageIdentities.Select(x => x.Id))}");

                _packageIdentities.Add(packageMetadata);

                var fetchIdentityTasks = new List<Task>();
                foreach (var dependentPackageIdentity in packageMetadata.DependentPackageIdentities)
                {
                    fetchIdentityTasks.Add(Task.Run(async ()
                        => _packageIdentities.AddRange(await GetPackageIdentities(
                            dependentPackageIdentity.Id,
                            dependentPackageIdentity.Version.ToString(),
                            targetFramework,
                            cancellationToken))));
                }

                Task.WaitAll(fetchIdentityTasks.ToArray());
            }

            return _packageIdentities;
        }

        public Task DownloadPackages(
            IEnumerable<PackageIdentity> packageIdentities,
            string outputPath,
            bool includePrerelease,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (packageIdentities != null && packageIdentities.Any())
                {
                    var packageToDownloadTask = new List<Task>();

                    packageIdentities.ToList().ForEach(packageIdentity =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        packageToDownloadTask.Add(Task.Run(async () =>
                        {
                            if (packageIdentity != null)
                            {
                                var settings = Settings.LoadDefaultSettings(outputPath, null, new MachineWideSettings());

                                var packageSourceProvider = new PackageSourceProvider(settings);
                                var sourceRepositoryProvider = new SourceRepositoryProvider(packageSourceProvider, _nuGetManager.ResourceProviders);

                                var project = new FolderNuGetProject(outputPath);
                                var packageManager = new NuGetPackageManager(sourceRepositoryProvider, settings, outputPath)
                                {
                                    PackagesFolderNuGetProject = project
                                };

                                var resolutionContext = new ResolutionContext(DependencyBehavior.Lowest, includePrerelease, false, VersionConstraints.None);
                                resolutionContext.SourceCacheContext.NoCache = resolutionContext.SourceCacheContext.DirectDownload = true;

                                bool packageAlreadyExists = packageManager.PackageExistsInPackagesFolder(packageIdentity.Identity, PackageSaveMode.None);
                                if (!packageAlreadyExists)
                                {
                                    var downloadContext = new PackageDownloadContext(resolutionContext.SourceCacheContext,
                                        outputPath, resolutionContext.SourceCacheContext.DirectDownload);

                                    var projectContext = new ProjectContext(_logger)
                                    {
                                        PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Files, XmlDocFileSaveMode.None, ClientPolicyContext.GetClientPolicy(settings, _logger), _logger)
                                    };

                                    await packageManager.InstallPackageAsync(
                                       project,
                                       packageIdentity.Identity,
                                       resolutionContext,
                                       projectContext,
                                       downloadContext,
                                       _nuGetManager.SourceRepository,
                                       Array.Empty<SourceRepository>(),
                                       CancellationToken.None);

                                    _logger.LogInformation($"Download of package {packageIdentity.Name}.{packageIdentity.Version.ToString()} is complete");
                                }
                            }
                        }));
                    });

                    Task.WaitAll(packageToDownloadTask.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Download packages failed due to the below errors:\nMessage:{ex.Message}\nStackTrace:{ex.StackTrace}");
                throw;
            }

            return Task.CompletedTask;
        }

        private async Task<T> GetPackageMetadataResource<T>() where T : class, INuGetResource
            => await _nuGetManager.SourceRepository.GetResourceAsync<T>();

        private async Task<PackageIdentity> GetPackageIdentity(
            string name,
            string version,
            TargetFramework targetFramework,
            CancellationToken cancellationToken)
        {
            PackageIdentity nuGetPackageIdentity = null;

            var packageSearchMetadata = await GetPackageMetadata(name, version, cancellationToken);

            if (packageSearchMetadata != null)
            {
                nuGetPackageIdentity = new PackageIdentity(packageSearchMetadata.Identity.Id, packageSearchMetadata.Identity.Version, packageSearchMetadata.Identity, GetDependentPackageIdentity(packageSearchMetadata, targetFramework));
            }

            return nuGetPackageIdentity;
        }

        private async Task<IPackageSearchMetadata> GetPackageMetadata(string name, string version, CancellationToken cancellationToken)
        {
            IPackageSearchMetadata packageSearchMetadata;

            var packageMetadataResource = await _nuGetManager.SourceRepository.GetResourceAsync<PackageMetadataResource>();

            if (NuGetVersion.TryParse(version, out var nugetVersion))
            {
                var packageIdentity = new NuGetCore.PackageIdentity(name, nugetVersion);

                var packageMetadata = await packageMetadataResource.GetMetadataAsync(
                    packageIdentity, _nuGetManager.SourceCacheContext, _logger, cancellationToken);

                if (packageMetadata == null)
                    _logger.LogInformation($"Package {name}.{version} not found");

                packageSearchMetadata = packageMetadata;
            }
            else
            {
                var packageMetadatas = await packageMetadataResource.GetMetadataAsync(
                    name, true, true, _nuGetManager.SourceCacheContext, _logger, cancellationToken);

                if (packageMetadatas.Any())
                    _logger.LogInformation($"Package {name} not found");

                packageSearchMetadata = packageMetadatas
                    .OrderByDescending(x => x.Identity.Version)
                    .FirstOrDefault();
            }

            return packageSearchMetadata;
        }

        private HashSet<NuGetCore.PackageIdentity> GetDependentPackageIdentity
            (IPackageSearchMetadata packageSearchMetadata, TargetFramework targetFramework)
        {
            var dependentPackageIdentities = new HashSet<NuGetCore.PackageIdentity>();

            if (packageSearchMetadata.DependencySets != null && packageSearchMetadata.DependencySets.Any())
            {
                var mostCompatibleFramework =
                    GetMostCompatibleFramework(targetFramework.ToNuGetFramework(), packageSearchMetadata.DependencySets);

                if (packageSearchMetadata.DependencySets.Any(x => x.TargetFramework == mostCompatibleFramework))
                {
                    var dependentSearchMetadata = packageSearchMetadata.DependencySets
                        .Where(x => x.TargetFramework == mostCompatibleFramework)
                        .FirstOrDefault();

                    if (dependentSearchMetadata != null && dependentSearchMetadata.Packages.Any())
                    {
                        dependentSearchMetadata.Packages.ToList().ForEach(x =>
                        {
                            dependentPackageIdentities.Add(
                                new NuGetCore.PackageIdentity(x.Id, x.VersionRange.MinVersion));
                        });
                    }
                }
            }

            return dependentPackageIdentities;
        }

        private NuGetFramework GetMostCompatibleFramework
            (NuGetFramework projectTargetFramework, IEnumerable<PackageDependencyGroup> itemGroups)
         => new FrameworkReducer().GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));
    }
}
