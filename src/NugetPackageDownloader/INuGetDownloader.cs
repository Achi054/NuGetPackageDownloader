using System;
using System.Threading.Tasks;

using NugetPackageDownloader.Constants;

namespace NugetPackageDownloader
{
    public interface INuGetDownloader
    {
        /// <summary>
        /// Retrieve list of available package versions
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        Task GetPackageVersionsAsync(
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
        Task DownloadPackageAsync(
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
        Task DownloadAndExtractPackageAsync(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            Action<NuGetDownloader> downloaderOptions = default);
    }
}
