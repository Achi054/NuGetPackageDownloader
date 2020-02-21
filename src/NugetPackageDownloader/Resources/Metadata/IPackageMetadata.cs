using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Protocol.Core.Types;

namespace NugetPackageDownloader.Resources.Metadata
{
    public interface IPackageMetadata
    {
        Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<PackageIdentity>> GetPackageIdentities(
            string name,
            string version,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);
    }
}
