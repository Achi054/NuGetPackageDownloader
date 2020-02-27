using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Protocol.Core.Types;

namespace NugetPackageDownloader.Resources.Metadata
{
    internal interface IPackageMetadata
    {
        Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadataAsync(
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
