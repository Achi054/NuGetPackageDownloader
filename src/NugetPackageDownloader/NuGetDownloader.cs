﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Core.Types;
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
        public async Task DownloadPackage(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            Action<NuGetDownloader> downloaderOptions = default)
        {
            downloaderOptions?.Invoke(this);
            await DownloadPackage(packageName, targetFramework, outputPath, CancellationToken);
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
                _logger?.LogInformation("Enter valid package name");
                throw new ArgumentException("Enter valid package name", nameof(packageName));
            }

            downloaderOptions?.Invoke(this);

            return await GetPackageSearchMetadata(packageName, targetFramework, CancellationToken);
        }

        /// <summary>
        /// Download and Extract the package content
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="targetFramework"></param>
        /// <param name="downloaderOptions"></param>
        /// <returns></returns>
        public async Task DownloadAndExtractPackage(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            Action<NuGetDownloader> downloaderOptions = null)
        {
            downloaderOptions?.Invoke(this);
            await DownloadAndExtractPackage(packageName, targetFramework, outputPath, CancellationToken);
        }

        private async Task DownloadAndExtractPackage(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation($"Downloading and extraction of package {packageName}.{Version} started");

            try
            {
                using var nuGetManager = new NuGetManager(targetFramework, true, null, IncludePrerelease, NuGetSourceRepositories, _logger);

                var packageIdentities = await _packageMetadata.GetPackageIdentities
                    (packageName, Version, nuGetManager, cancellationToken);

                await _packageDownloader.DownloadPackages
                    (packageIdentities, nuGetManager, cancellationToken);

                await _packageDownloader.ExtractPackageAssemblies
                    (outputPath, packageIdentities, nuGetManager, cancellationToken);
            }
            catch (Exception)
            {
                _logger?.LogError($"Downloading and extraction of package {packageName}.{Version} failed.");
            }

            _logger?.LogInformation($"Download and extraction of package {packageName}.{Version} completed");
        }

        private async Task<IEnumerable<IPackageSearchMetadata>> GetPackageSearchMetadata(
            string packageName,
            TargetFramework targetFramework,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                _logger?.LogInformation("Enter valid package name");
                throw new ArgumentException("Enter valid package name", nameof(packageName));
            }

            IEnumerable<IPackageSearchMetadata> packageMetadata = null;

            _logger?.LogInformation($"Fetching package metadata for {packageName} started");

            try
            {
                using var nuGetManager = new NuGetManager(targetFramework, false, null, IncludePrerelease, NuGetSourceRepositories, _logger);

                packageMetadata = await _packageMetadata.GetPackageSearchMetadata(packageName, nuGetManager, cancellationToken);
            }
            catch (Exception)
            {
                _logger?.LogError($"Fetching package metadata for {packageName} failed.");
            }

            _logger?.LogInformation($"Fetching package metadata for {packageName} completed");

            return packageMetadata;
        }

        private async Task DownloadPackage(
            string packageName,
            TargetFramework targetFramework,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation($"Downloading package {packageName}.{Version} started");

            try
            {
                using var nuGetManager = new NuGetManager(targetFramework, false, outputPath, IncludePrerelease, NuGetSourceRepositories, _logger);

                var packageIdentities = await _packageMetadata.GetPackageIdentities(
                    packageName, Version, nuGetManager, cancellationToken);

                await _packageDownloader.DownloadPackages(packageIdentities, nuGetManager, cancellationToken);
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
