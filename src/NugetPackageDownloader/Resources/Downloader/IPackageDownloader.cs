using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NugetPackageDownloader.Resources.Downloader
{
    public interface IPackageDownloader
    {
        Task DownloadPackages(
            IEnumerable<PackageIdentity> packageIdentities,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);
    }
}
