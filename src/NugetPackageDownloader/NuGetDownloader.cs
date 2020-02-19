using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Protocol.Core.Types;

using NugetPackageDownloader.Helpers;
using IPackageMetadata = NugetPackageDownloader.Resources.Metadata.IPackageMetadata;

namespace NugetPackageDownloader
{
    public class NuGetDownloader : INuGetDownloader
    {
        private const string NuGetPath = "nuget";

        private readonly ILogger _logger;
        private readonly IPackageMetadata _packageMetadata;

        public NuGetDownloader(ILogger logger, IPackageMetadata packageMetadata)
            => (_logger, _packageMetadata) = (logger, packageMetadata);

        public string Version { get; set; } = default;
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
        public string OutputPath { get; set; } = NuGetPath;
        public bool IncludePrerelease { get; set; } = default;

        /// <summary>
        /// Method to download NuGet package
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        public async Task DownloadPackage(
            string packageName,
            TargetFramework targetFramework,
            Action<NuGetDownloader> downloaderOptions = default)
        {
            downloaderOptions?.Invoke(this);
            await DownloadPackage(packageName, targetFramework, Version, OutputPath, IncludePrerelease, CancellationToken);
        }

        /// <summary>
        /// Method to fetch package metadata
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            TargetFramework targetFramework,
            Action<NuGetDownloader> downloaderOptions = default)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                _logger.LogInformation("Enter valid package name");
                throw new ArgumentException("Enter valid package name", nameof(packageName));
            }

            downloaderOptions?.Invoke(this);

            return await GetPackageSearchMetadata(packageName, targetFramework, IncludePrerelease, CancellationToken);
        }

        private async Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            TargetFramework targetFramework,
            bool includePrerelease = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                _logger.LogInformation("Enter valid package name");
                throw new ArgumentException("Enter valid package name", nameof(packageName));
            }

            IEnumerable<IPackageSearchMetadata> packageMetadata = null;

            _logger.LogInformation($"Fetching package metadata for {packageName} started");

            try
            {
                packageMetadata = await _packageMetadata.GetPackageSearchMetadata(packageName, targetFramework, includePrerelease, cancellationToken);
            }
            catch (Exception)
            {
                _logger.LogError($"Fetching package metadata for {packageName} failed.");
            }

            _logger.LogInformation($"Fetching package metadata for {packageName} completed");

            return packageMetadata;
        }

        private async Task DownloadPackage(
            string packageName,
            TargetFramework targetFramework,
            string version,
            string outputPath,
            bool includePrerelease = default,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Downloading package {packageName}.{version} started");

            try
            {
                var packageIdentities = await _packageMetadata.GetPackageIdentities(
                    packageName, version, targetFramework, cancellationToken);

                await _packageMetadata.DownloadPackages(packageIdentities, outputPath, includePrerelease);
            }
            catch (Exception)
            {
                _logger.LogError($"Downloading package {packageName}.{version} failed.");
            }

            _logger.LogInformation($"Downloading package {packageName}.{version} completed");
        }
    }
}
