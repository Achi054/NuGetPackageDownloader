using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NuGet.Protocol.Core.Types;

using NugetPackageDownloader.Constants;

namespace NugetPackageDownloader
{
    public interface INuGetDownloader
    {
        /// <summary>
        /// Retrieve the NuGet package metadata
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            TargetFramework targetFramework,
            Action<NuGetDownloader> downloaderOptions = default);

        /// <summary>
        /// Download the NuGet package to the desired location
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="outputPath"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        Task DownloadPackage(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            Action<NuGetDownloader> downloaderOptions = default);

        /// <summary>
        /// Download and extract the NuGet package and dependent assemblies to the desired path
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="outputPath"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        Task DownloadAndExtractPackage(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            Action<NuGetDownloader> downloaderOptions = default);
    }
}
