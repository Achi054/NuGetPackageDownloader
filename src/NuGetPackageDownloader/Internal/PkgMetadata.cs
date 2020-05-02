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

                IEnumerable<NuGetVersion> versions = (await resource
                    .GetAllVersionsAsync(packageName,
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

            foreach (SourceRepository nuGetSourceRepository in manager.SourceRepositories)
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
                : new PkgIdentity(packageSearchMetadata.Identity,
                    GetDependentPackageIdentity(packageSearchMetadata, manager.Framework));
        }

        private async Task<IPackageSearchMetadata?> GetPackageMetadataAsync(ICollection<PkgIdentity> identities,
            string name,
            string? version,
            NuGetManager manager,
            SourceRepository sourceRepository,
            CancellationToken cancellationToken)
        {
            if (identities.Any(x => x.Name == name && x.Version.ToString() == version))
                return null;

            PackageMetadataResource packageMetadataResource = await GetPackageMetadataResourceAsync<PackageMetadataResource>(sourceRepository);

            if (NuGetVersion.TryParse(version, out NuGetVersion nugetVersion))
            {
                var packageIdentity = new PackageIdentity(name, nugetVersion);

                IPackageSearchMetadata metadata = await packageMetadataResource.GetMetadataAsync(
                    packageIdentity, manager.SourceCacheContext, _logger, cancellationToken);

                return metadata;
            }
            else
            {
                IEnumerable<IPackageSearchMetadata> packageMetadatas = await packageMetadataResource
                    .GetMetadataAsync(name, true, true, manager.SourceCacheContext, _logger, cancellationToken);

                IPackageSearchMetadata metadata = packageMetadatas
                    .OrderByDescending(x => x.Identity.Version)
                    .FirstOrDefault();

                return metadata;
            }
        }

        private async Task<T> GetPackageMetadataResourceAsync<T>(SourceRepository nuGetRepository)
            where T : class, INuGetResource
        {
            return await nuGetRepository.GetResourceAsync<T>();
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
