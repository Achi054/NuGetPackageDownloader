using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Protocol.Core.Types;

using NugetPackageDownloader.Helpers;

namespace NugetPackageDownloader.Resources.Metadata
{
    public interface IPackageMetadata
    {
        Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            NuGetManager nuGetManager,
            TargetFramework targetFramework = TargetFramework.NETSTANDARD2_1,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<PackageIdentity>> GetPackageIdentities(
            string name,
            string version,
            NuGetManager nuGetManager,
            TargetFramework targetFramework = TargetFramework.NETSTANDARD2_1,
            CancellationToken cancellationToken = default);
    }
}
