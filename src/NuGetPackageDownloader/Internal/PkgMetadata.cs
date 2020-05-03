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
                    .Where(ver => ver.IsPrerelease == manager.IncludePrerelease);

                result = result.Concat(versions.Select(v => v.ToNormalizedString()));
            }

            return result;
        }

        internal async Task<IEnumerable<PkgIdentity>> GetPackageIdentitiesAsync(string name,
            string? version,
            NuGetManager manager,
            CancellationToken cancellationToken)
        {
            var identities = new PkgIdentities();

            foreach (SourceRepository sourceRepository in manager.SourceRepositories)
            {
                PkgIdentity? identity = await GetPackageIdentityAsync(identities, name, version, manager,
                    sourceRepository, cancellationToken);

                if (identity is null)
                    continue;

                identities.Add(identity);

                foreach (PackageIdentity dependentPackageIdentity in identity.DependentPackageIdentities)
                {
                    identities.AddRange(await GetPackageIdentitiesAsync(
                        dependentPackageIdentity.Id,
                        dependentPackageIdentity.Version.ToString(),
                        manager,
                        cancellationToken));
                }
            }

            return identities;
        }

        private async Task<PkgIdentity?> GetPackageIdentityAsync(ICollection<PkgIdentity> identities,
            string name,
            string? version,
            NuGetManager manager,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            // If the specified package ID already exists in the collection, no need to get its metadata
            // again.
            if (identities.Any(id => id.Name == name && id.Version.ToString() == version))
                return null;

            cancellationToken.ThrowIfCancellationRequested();

            IPackageSearchMetadata packageMetadata = await GetPackageMetadataAsync(name,
                version, manager, sourceRepository, cancellationToken);
            return new PkgIdentity(packageMetadata.Identity,
                GetDependentPackageIdentity(packageMetadata, manager.Framework));
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

        private HashSet<PackageIdentity> GetDependentPackageIdentity(IPackageSearchMetadata packageMetadata,
            NuGetFramework targetFramework)
        {
            var dependentPackageIdentities = new HashSet<PackageIdentity>();

            if (packageMetadata.DependencySets is null || !packageMetadata.DependencySets.Any())
                return dependentPackageIdentities;

            NuGetFramework mostCompatibleFramework = GetMostCompatibleFramework(targetFramework,
                packageMetadata.DependencySets);

            if (!packageMetadata.DependencySets.Any(x => x.TargetFramework == mostCompatibleFramework))
                return dependentPackageIdentities;

            PackageDependencyGroup dependentPackages = packageMetadata.DependencySets
                .Where(x => x.TargetFramework == mostCompatibleFramework)
                .FirstOrDefault();

            if (dependentPackages is null || !dependentPackages.Packages.Any())
                return dependentPackageIdentities;

            foreach (PackageDependency dependentPackage in dependentPackages.Packages)
                dependentPackageIdentities.Add(new PackageIdentity(dependentPackage.Id, dependentPackage.VersionRange.MinVersion));

            return dependentPackageIdentities;
        }

        private NuGetFramework GetMostCompatibleFramework(NuGetFramework projectTargetFramework,
            IEnumerable<PackageDependencyGroup> itemGroups)
        {
            return new FrameworkReducer().GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));
        }
    }
}
