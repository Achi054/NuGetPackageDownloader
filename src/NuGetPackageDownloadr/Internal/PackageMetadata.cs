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
    internal sealed class PackageMetadata
    {
        private readonly ILogger _logger;

        internal PackageMetadata(ILogger logger)
        {
            _logger = logger;
        }

        internal async Task<IEnumerable<string>> GetPackageVersionsAsync(string packageName,
            NuGetManager manager,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<string> result = Enumerable.Empty<string>();

            foreach (SourceRepository sourceRepository in manager.NuGetSourceRepositories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FindPackageByIdResource resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

                IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                    packageName,
                    manager.NuGetSourceCacheContext,
                    _logger,
                    cancellationToken);

                result = result.Concat(versions.Select(v => v.ToNormalizedString()));
            }

            return result;
        }

        internal async Task<IEnumerable<PkgIdentity>> GetPackageIdentitiesAsync(string name,
            string? version,
            NuGetManager manager,
            CancellationToken cancellationToken = default)
        {
            var identities = new HashSet<PkgIdentity>();

            foreach (SourceRepository nuGetSourceRepository in manager.NuGetSourceRepositories)
            {
                PkgIdentity? identity = await GetPackageIdentityAsync(identities, name, version,
                    manager, nuGetSourceRepository, cancellationToken);

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

        private async Task<T> GetPackageMetadataResourceAsync<T>(SourceRepository nuGetRepository)
            where T : class, INuGetResource
        {
            return await nuGetRepository.GetResourceAsync<T>();
        }

        private async Task<PkgIdentity?> GetPackageIdentityAsync(ICollection<PkgIdentity> identities,
            string name,
            string? version,
            NuGetManager manager,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            IPackageSearchMetadata? packageSearchMetadata = await GetPackageMetadataAsync(
                identities, name, version, manager, sourceRepository, cancellationToken);
            return packageSearchMetadata is null
                ? null
                : new PkgIdentity(
                    packageSearchMetadata.Identity.Id,
                    packageSearchMetadata.Identity.Version,
                    packageSearchMetadata.Identity,
                    GetDependentPackageIdentity(packageSearchMetadata, manager.NuGetFramework));
        }

        private async Task<IPackageSearchMetadata?> GetPackageMetadataAsync(ICollection<PkgIdentity> identities,
            string name,
            string? version,
            NuGetManager manager,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            IPackageSearchMetadata? packageSearchMetadata = null;

            if (!identities.Any(x => x.Name == name && x.Version.ToString() == version))
            {
                PackageMetadataResource packageMetadataResource = await GetPackageMetadataResourceAsync<PackageMetadataResource>(sourceRepository);

                if (NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    var packageIdentity = new PackageIdentity(name, nugetVersion);

                    IPackageSearchMetadata packageMetadata = await packageMetadataResource.GetMetadataAsync(
                        packageIdentity, manager.NuGetSourceCacheContext, _logger, cancellationToken);

                    packageSearchMetadata = packageMetadata;
                }
                else
                {
                    IEnumerable<IPackageSearchMetadata> packageMetadatas = await packageMetadataResource
                        .GetMetadataAsync(name, true, true, manager.NuGetSourceCacheContext, _logger, cancellationToken);

                    packageSearchMetadata = packageMetadatas
                        .OrderByDescending(x => x.Identity.Version)
                        .FirstOrDefault();
                }
            }

            return packageSearchMetadata;
        }

        private HashSet<PackageIdentity> GetDependentPackageIdentity(IPackageSearchMetadata packageSearchMetadata,
            NuGetFramework targetFramework)
        {
            var dependentPackageIdentities = new HashSet<PackageIdentity>();

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
                        dependentSearchMetadata.Packages.ToList().ForEach(x =>
                        {
                            dependentPackageIdentities.Add(
                                new PackageIdentity(x.Id, x.VersionRange.MinVersion));
                        });
                }
            }

            return dependentPackageIdentities;
        }

        private NuGetFramework GetMostCompatibleFramework(NuGetFramework projectTargetFramework,
            IEnumerable<PackageDependencyGroup> itemGroups)
        {
            return new FrameworkReducer().GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));
        }
    }
}
