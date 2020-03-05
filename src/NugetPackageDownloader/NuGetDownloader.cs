using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NugetPackageDownloader.Constants;
using NugetPackageDownloader.Logging;
using NugetPackageDownloader.Resources;
using NugetPackageDownloader.Resources.Downloader;
using NugetPackageDownloader.Resources.Metadata;
using IPackageMetadata = NugetPackageDownloader.Resources.Metadata.IPackageMetadata;
using NugetLogger = NuGet.Common;

namespace NugetPackageDownloader
{
    public class NuGetDownloader : INuGetDownloader
    {
        private readonly NugetLogger.ILogger _logger;
        private readonly IPackageMetadata _packageMetadata;
        private readonly IPackageDownloader _packageDownloader;

        public NuGetDownloader(NugetLogger.ILogger logger = default)
        {
            _logger = logger ?? ComposeLogger();
            _packageMetadata = new PackageMetadata(_logger);
            _packageDownloader = new PackageDownloader(_logger);
        }

        public string Version { get; set; } = default;
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
        public string OutputPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        public bool IncludePrerelease { get; set; } = default;
        public IEnumerable<string> NuGetSourceRepositories { get; set; }

        /// <summary>
        /// Method to download NuGet package
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        public async Task DownloadPackageAsync(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            Action<NuGetDownloader> downloaderOptions = default)
        {
            downloaderOptions?.Invoke(this);
            await DownloadPackageAsync(packageName, targetFramework, outputPath, CancellationToken);
        }

        /// <summary>
        /// Method to retrieve all the package versions
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        public async Task GetPackageVersionsAsync(
            string packageName,
            TargetFramework targetFramework,
            Action<NuGetDownloader> downloaderOptions = default)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                _logger?.LogInformation("Enter valid package name");
                throw new ArgumentException("Enter valid package name", nameof(packageName));
            }

            downloaderOptions?.Invoke(this);

            await GetPackageVersionsAsync(packageName, targetFramework, CancellationToken);
        }

        /// <summary>
        /// Download and Extract the package content
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        public async Task DownloadAndExtractPackageAsync(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            Action<NuGetDownloader> downloaderOptions = null)
        {
            downloaderOptions?.Invoke(this);
            await DownloadAndExtractPackageAsync(packageName, targetFramework, outputPath, CancellationToken);
        }

        private async Task DownloadAndExtractPackageAsync(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation($"Downloading and extraction of package {packageName}.{Version} started");

            try
            {
                var nuGetManager = new NuGetManager(targetFramework, true, null, IncludePrerelease, NuGetSourceRepositories, _logger);

                var packageIdentities = await _packageMetadata.GetPackageIdentitiesAsync
                    (packageName, Version, nuGetManager, cancellationToken);

                await _packageDownloader.DownloadPackagesAsync
                    (packageIdentities, nuGetManager, cancellationToken);

                await _packageDownloader.ExtractPackageAssembliesAsync
                    (outputPath, packageIdentities, nuGetManager, cancellationToken);
            }
            catch (Exception)
            {
                _logger?.LogError($"Downloading and extraction of package {packageName}.{Version} failed.");
            }

            _logger?.LogInformation($"Download and extraction of package {packageName}.{Version} completed");
        }

        private async Task GetPackageVersionsAsync(
            string packageName,
            TargetFramework targetFramework,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                _logger?.LogInformation("Enter valid package name");
                throw new ArgumentException("Enter valid package name", nameof(packageName));
            }

            _logger?.LogInformation($"Retreiving package versions for {packageName} started");

            try
            {
                var nuGetManager = new NuGetManager(targetFramework, false, null, IncludePrerelease, NuGetSourceRepositories, _logger);

                await _packageMetadata.GetPackageVersionsAsync(packageName, nuGetManager, cancellationToken);
            }
            catch (Exception)
            {
                _logger?.LogError($"Retreiving package versions for {packageName} failed.");
            }

            _logger?.LogInformation($"Retreiving package versoins for {packageName} completed");
        }

        private async Task DownloadPackageAsync(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation($"Downloading package {packageName}.{Version} started");

            try
            {
                var nuGetManager = new NuGetManager(targetFramework, false, outputPath, IncludePrerelease, NuGetSourceRepositories, _logger);

                var packageIdentities = await _packageMetadata.GetPackageIdentitiesAsync(
                    packageName, Version, nuGetManager, cancellationToken);

                await _packageDownloader.DownloadPackagesAsync(packageIdentities, nuGetManager, cancellationToken);
            }
            catch (Exception)
            {
                _logger?.LogError($"Downloading package {packageName}.{Version} failed.");
            }

            _logger?.LogInformation($"Downloading package {packageName}.{Version} completed");
        }

        private NugetLogger.ILogger ComposeLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("NugetPackageDownloader.NuGetDownloader", LogLevel.Information)
                    .AddConsole();
            });
            return new NuGetLogger(loggerFactory.CreateLogger<NuGetDownloader>());
        }
    }
}
