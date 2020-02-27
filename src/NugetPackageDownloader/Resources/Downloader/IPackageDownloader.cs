using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NugetPackageDownloader.Resources.Downloader
{
    internal interface IPackageDownloader
    {
        Task DownloadPackagesAsync(
            IEnumerable<PackageIdentity> packageIdentities,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);

        Task ExtractPackageAssembliesAsync(
            string outputPath,
            IEnumerable<PackageIdentity> packageIdentities,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);
    }
}
