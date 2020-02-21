using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetCore = NuGet.Packaging.Core;

namespace NugetPackageDownloader.Resources.Metadata
{
    public class PackageMetadata : IPackageMetadata
    {
        private readonly ILogger _logger;

        public PackageMetadata(ILogger logger) => _logger = logger;

        public async Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default)
        {
            var packageSearchMetadatas = new HashSet<IPackageSearchMetadata>();

            _logger.LogInformation($"Searching package metadata for {packageName}");

            foreach (var nuGetRepository in nuGetManager.NuGetSourceRepositories)
            {
                try
                {
                    var packageSearchResource = await GetPackageMetadataResource<PackageSearchResource>(nuGetRepository);

                    var searchFilter = new SearchFilter(nuGetManager.IncludePrerelease, SearchFilterType.IsLatestVersion)
                    {
                        SupportedFrameworks = new[] { nuGetManager.NuGetFramework.ToString() },
                        IncludeDelisted = false,
                        OrderBy = SearchOrderBy.Id
                    };

                    packageSearchMetadatas.AddRange(await packageSearchResource.SearchAsync(packageName, searchFilter, 0, 10, _logger, cancellationToken));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    continue;
                }
            }

            return packageSearchMetadatas;
        }

        public async Task<IEnumerable<PackageIdentity>> GetPackageIdentities(
            string name,
            string version,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Fetching package identities");

            var packageIdentities = new HashSet<PackageIdentity>();

            foreach (var nuGetSourceRepository in nuGetManager.NuGetSourceRepositories)
            {
                try
                {
                    var packageMetadata = await GetPackageIdentity(name, version,
                        nuGetManager, nuGetSourceRepository, cancellationToken);

                    if (packageMetadata != null && packageMetadata.DependentPackageIdentities.Any())
                    {
                        _logger.LogInformation($"Package Name: {packageMetadata.Name}\nPackage Version: {packageMetadata.Identity.Version.ToString()}\nDependencies:\n{string.Join("\n", packageMetadata.DependentPackageIdentities.Select(x => x.Id))}");

                        packageIdentities.Add(packageMetadata);

                        var fetchIdentityTasks = new List<Task>();

                        foreach (var dependentPackageIdentity in packageMetadata.DependentPackageIdentities)
                        {
                            fetchIdentityTasks.Add(Task.Run(async ()
                                => packageIdentities.AddRange(await GetPackageIdentities(
                                    dependentPackageIdentity.Id,
                                    dependentPackageIdentity.Version.ToString(),
                                    nuGetManager,
                                    cancellationToken))));
                        }

                        Task.WaitAll(fetchIdentityTasks.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    continue;
                }
            }

            return packageIdentities;
        }

        private async Task<T> GetPackageMetadataResource<T>(SourceRepository nuGetRepository) where T : class, INuGetResource
            => await nuGetRepository.GetResourceAsync<T>();

        private async Task<PackageIdentity> GetPackageIdentity(
            string name,
            string version,
            NuGetManager nuGetManager,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            PackageIdentity nuGetPackageIdentity = null;

            var packageSearchMetadata = await GetPackageMetadata(name, version, nuGetManager, sourceRepository, cancellationToken);

            if (packageSearchMetadata != null)
            {
                nuGetPackageIdentity = new PackageIdentity(packageSearchMetadata.Identity.Id, packageSearchMetadata.Identity.Version, packageSearchMetadata.Identity, GetDependentPackageIdentity(packageSearchMetadata, nuGetManager.NuGetFramework));
            }

            return nuGetPackageIdentity;
        }

        private async Task<IPackageSearchMetadata> GetPackageMetadata(
            string name,
            string version,
            NuGetManager nuGetManager,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            IPackageSearchMetadata packageSearchMetadata;

            var packageMetadataResource = await GetPackageMetadataResource<PackageMetadataResource>(sourceRepository);

            if (NuGetVersion.TryParse(version, out var nugetVersion))
            {
                var packageIdentity = new NuGetCore.PackageIdentity(name, nugetVersion);

                var packageMetadata = await packageMetadataResource.GetMetadataAsync(
                    packageIdentity, nuGetManager.NuGetSourceCacheContext, _logger, cancellationToken);

                if (packageMetadata == null)
                    _logger.LogInformation($"Package {name}.{version} not found");

                packageSearchMetadata = packageMetadata;
            }
            else
            {
                var packageMetadatas = await packageMetadataResource.GetMetadataAsync(
                    name, true, true, nuGetManager.NuGetSourceCacheContext, _logger, cancellationToken);

                if (packageMetadatas.Any())
                    _logger.LogInformation($"Package {name} not found");

                packageSearchMetadata = packageMetadatas
                    .OrderByDescending(x => x.Identity.Version)
                    .FirstOrDefault();
            }

            return packageSearchMetadata;
        }

        private HashSet<NuGetCore.PackageIdentity> GetDependentPackageIdentity
            (IPackageSearchMetadata packageSearchMetadata, NuGetFramework targetFramework)
        {
            var dependentPackageIdentities = new HashSet<NuGetCore.PackageIdentity>();

            if (packageSearchMetadata.DependencySets != null && packageSearchMetadata.DependencySets.Any())
            {
                var mostCompatibleFramework =
                    GetMostCompatibleFramework(targetFramework, packageSearchMetadata.DependencySets);

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
