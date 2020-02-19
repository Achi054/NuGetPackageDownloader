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
            TargetFramework targetFramework = TargetFramework.NETSTANDARD2_1,
            bool includePrerelease = default,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<PackageIdentity>> GetPackageIdentities(
            string name,
            string version,
            TargetFramework targetFramework = TargetFramework.NETSTANDARD2_1,
            CancellationToken cancellationToken = default);

        Task DownloadPackages(
            IEnumerable<PackageIdentity> packageIdentities,
            string outputPath, bool includePrerelease,
            CancellationToken cancellationToken = default);
    }
}
