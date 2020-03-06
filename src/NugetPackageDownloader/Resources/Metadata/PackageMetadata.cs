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
    internal class PackageMetadata : IPackageMetadata
    {
        private readonly ILogger _logger;
        private ICollection<PackageIdentity> _packageIdentities;

        public PackageMetadata(ILogger logger = default) => (_logger, _packageIdentities) = (logger, new HashSet<PackageIdentity>());

        public async Task GetPackageVersionsAsync(
            string packageName,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation($"Searching package versions for {packageName}");

            foreach (var nuGetRepository in nuGetManager.NuGetSourceRepositories)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    FindPackageByIdResource resource = await nuGetRepository.GetResourceAsync<FindPackageByIdResource>();

                    IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                        packageName,
                        nuGetManager.NuGetSourceCacheContext,
                        _logger,
                        cancellationToken);

                    if (versions != null && versions.Any())
                        _logger.LogInformation($"{packageName} package versions:");
                    foreach (NuGetVersion version in versions)
                    {
                        _logger.LogInformation($"{version}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    continue;
                }
            }
        }

        public async Task<IEnumerable<PackageIdentity>> GetPackageIdentitiesAsync(
            string name,
            string version,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation($"Fetching package identities");

            foreach (var nuGetSourceRepository in nuGetManager.NuGetSourceRepositories)
            {
                try
                {
                    var packageMetadata = await GetPackageIdentityAsync(name, version,
                        nuGetManager, nuGetSourceRepository, cancellationToken);

                    if (packageMetadata != null)
                    {
                        _packageIdentities.Add(packageMetadata);

                        if (packageMetadata.DependentPackageIdentities.Any())
                        {
                            _logger?.LogInformation($"Package Name: {packageMetadata.Name}\nPackage Version: {packageMetadata.Identity.Version.ToString()}\nDependencies:\n{string.Join("\n", packageMetadata.DependentPackageIdentities.Select(x => x.Id))}");


                            foreach (var dependentPackageIdentity in packageMetadata.DependentPackageIdentities)
                            {
                                _packageIdentities.AddRange(await GetPackageIdentitiesAsync(
                                        dependentPackageIdentity.Id,
                                        dependentPackageIdentity.Version.ToString(),
                                        nuGetManager,
                                        cancellationToken));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    continue;
                }
            }

            return _packageIdentities;
        }

        private async Task<T> GetPackageMetadataResourceAsync<T>(SourceRepository nuGetRepository) where T : class, INuGetResource
            => await nuGetRepository.GetResourceAsync<T>();

        private async Task<PackageIdentity> GetPackageIdentityAsync(
            string name,
            string version,
            NuGetManager nuGetManager,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            PackageIdentity nuGetPackageIdentity = null;

            var packageSearchMetadata = await GetPackageMetadataAsync(name, version, nuGetManager, sourceRepository, cancellationToken);

            if (packageSearchMetadata != null)
            {
                nuGetPackageIdentity = new PackageIdentity(packageSearchMetadata.Identity.Id, packageSearchMetadata.Identity.Version, packageSearchMetadata.Identity, GetDependentPackageIdentity(packageSearchMetadata, nuGetManager.NuGetFramework));
            }

            return nuGetPackageIdentity;
        }

        private async Task<IPackageSearchMetadata> GetPackageMetadataAsync(
            string name,
            string version,
            NuGetManager nuGetManager,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            IPackageSearchMetadata packageSearchMetadata = null;

            if (!_packageIdentities.Any(x => x.Name == name && x.Version.ToString() == version))
            {
                var packageMetadataResource = await GetPackageMetadataResourceAsync<PackageMetadataResource>(sourceRepository);

                if (NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    var packageIdentity = new NuGetCore.PackageIdentity(name, nugetVersion);

                    var packageMetadata = await packageMetadataResource.GetMetadataAsync(
                        packageIdentity, nuGetManager.NuGetSourceCacheContext, _logger, cancellationToken);

                    if (packageMetadata == null)
                        _logger?.LogInformation($"Package {name}.{version} not found");

                    packageSearchMetadata = packageMetadata;
                }
                else
                {
                    var packageMetadatas = await packageMetadataResource.GetMetadataAsync(
                        name, true, true, nuGetManager.NuGetSourceCacheContext, _logger, cancellationToken);

                    if (packageMetadatas.Any())
                        _logger?.LogInformation($"Package {name} not found");

                    packageSearchMetadata = packageMetadatas
                        .OrderByDescending(x => x.Identity.Version)
                        .FirstOrDefault();
                }
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
