using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NugetPackageDownloader.Resources.Metadata
{
    internal interface IPackageMetadata
    {
        Task GetPackageVersionsAsync(
            string packageName,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<PackageIdentity>> GetPackageIdentitiesAsync(
            string name,
            string version,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);
    }
}
