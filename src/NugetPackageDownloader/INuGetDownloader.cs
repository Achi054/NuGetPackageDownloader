using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NuGet.Protocol.Core.Types;

using NugetPackageDownloader.Helpers;

namespace NugetPackageDownloader
{
    public interface INuGetDownloader
    {
        Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            TargetFramework targetFramework,
            Action<NuGetDownloader> downloaderOptions = default);

        Task DownloadPackage(
            string packageName,
            TargetFramework targetFramework,
            Action<NuGetDownloader> downloaderOptions = default);
    }
}
