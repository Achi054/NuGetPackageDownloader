using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Protocol.Core.Types;

using NugetPackageDownloader.Helpers;
using NugetPackageDownloader.Resources;
using NugetPackageDownloader.Resources.Downloader;
using IPackageMetadata = NugetPackageDownloader.Resources.Metadata.IPackageMetadata;

namespace NugetPackageDownloader
{
    public class NuGetDownloader : INuGetDownloader
    {
        private readonly ILogger _logger;
        private readonly IPackageMetadata _packageMetadata;
        private readonly IPackageDownloader _packageDownloader;

        public NuGetDownloader(ILogger logger, IPackageMetadata packageMetadata, IPackageDownloader packageDownloader)
            => (_logger, _packageMetadata, _packageDownloader) = (logger, packageMetadata, packageDownloader);

        public string Version { get; set; } = default;
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
        public string OutputPath { get; set; } = default;
        public bool IncludePrerelease { get; set; } = default;
        public IEnumerable<string> NuGetSourceRepositories { get; set; }

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
            await DownloadPackage(packageName, targetFramework, CancellationToken);
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

            return await GetPackageSearchMetadata(packageName, targetFramework, CancellationToken);
        }

        private async Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            TargetFramework targetFramework,
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
                var nuGetManager = new NuGetManager(_logger, IncludePrerelease, NuGetSourceRepositories);

                packageMetadata = await _packageMetadata.GetPackageSearchMetadata(packageName, nuGetManager, targetFramework, cancellationToken);
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
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Downloading package {packageName}.{Version} started");

            try
            {
                var nuGetManager = new NuGetManager(_logger, IncludePrerelease, NuGetSourceRepositories);

                var packageIdentities = await _packageMetadata.GetPackageIdentities(
                    packageName, Version, nuGetManager, targetFramework, cancellationToken);

                await _packageDownloader.DownloadPackages(packageIdentities, nuGetManager, cancellationToken);
            }
            catch (Exception)
            {
                _logger.LogError($"Downloading package {packageName}.{Version} failed.");
            }

            _logger.LogInformation($"Downloading package {packageName}.{Version} completed");
        }
    }
}
