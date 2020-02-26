using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NugetPackageDownloader.Resources.Downloader
{
    internal interface IPackageDownloader
    {
        Task DownloadPackages(
            IEnumerable<PackageIdentity> packageIdentities,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);

        Task ExtractPackageAssemblies(
            string outputPath,
            IEnumerable<PackageIdentity> packageIdentities,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default);
    }
}
