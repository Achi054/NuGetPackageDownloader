using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGetPackageDownloader.Internal
{
    internal sealed class PkgMetadata
    {
        private readonly ILogger _logger;

        internal PkgMetadata(ILogger logger)
        {
            _logger = logger;
        }

        internal async Task<IEnumerable<string>> GetPackageVersionsAsync(string packageName,
            NuGetManager manager,
            CancellationToken cancellationToken)
        {
            IEnumerable<string> result = Enumerable.Empty<string>();

            foreach (SourceRepository sourceRepository in manager.SourceRepositories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FindPackageByIdResource resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

                IEnumerable<NuGetVersion> versions = (await resource.GetAllVersionsAsync(packageName,
                        manager.SourceCacheContext,
                        _logger,
                        cancellationToken))
                    .Where(ver => !ver.IsPrerelease || manager.IncludePrerelease);

                result = result.Concat(versions.Select(v => v.ToNormalizedString()));
            }

            return result;
        }

        internal async Task<IReadOnlyList<PackageIdentity>> GetPackageIdentitiesAsync(string name,
            string? version,
            NuGetManager manager,
            CancellationToken cancellationToken)
        {
            var metadatas = new PackageMetadataCollection();

            IPackageSearchMetadata? metadata = await GetPackageMetadataAsync(name, version, manager, cancellationToken);
            if (metadata is null)
                return Array.Empty<PackageIdentity>();

            metadatas.Add(metadata);

            if (manager.Recursive)
            {
                int index = 0;
                while (index < metadatas.Count)
                {
                    IPackageSearchMetadata metadataAtIndex = metadatas[index];
                    IEnumerable<PackageIdentity> dependentPackages = GetDependentPackageIdentities(metadataAtIndex, manager.Framework);
                    foreach (PackageIdentity dependentPackage in dependentPackages)
                    {
                        IPackageSearchMetadata? dependentMetadata = await GetPackageMetadataAsync(dependentPackage.Id,
                            dependentPackage.Version.ToNormalizedString(),
                            manager,
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
            NuGetManager manager,
            CancellationToken cancellationToken)
        {
            foreach (SourceRepository sourceRepository in manager.SourceRepositories)
            {
                IPackageSearchMetadata packageMetadata = await GetPackageMetadataAsync(name,
                    version, manager, sourceRepository, cancellationToken);
                if (packageMetadata != null)
                    return packageMetadata;
            }

            return default;
        }

        private async Task<IPackageSearchMetadata> GetPackageMetadataAsync(string name,
            string? version,
            NuGetManager manager,
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
                    manager.SourceCacheContext,
                    _logger,
                    cancellationToken);
            }

            // If version is not specified, get metadata for all packages and return the latest.
            IEnumerable<IPackageSearchMetadata> packageMetadatas = await packageMetadataResource.GetMetadataAsync(
                name,
                manager.IncludePrerelease,
                includeUnlisted: false,
                manager.SourceCacheContext,
                _logger,
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
}
