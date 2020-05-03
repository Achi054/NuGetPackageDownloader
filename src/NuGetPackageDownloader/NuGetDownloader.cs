using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Packaging.Core;
using NuGetPackageDownloader.Internal;

namespace NuGetPackageDownloader
{
    public class NuGetDownloader
    {
        private readonly ILogger _logger = new NullLogger();
        private readonly PkgMetadata _packageMetadata;
        private readonly PkgDownloader _packageDownloader;

        public NuGetDownloader(string? outputPath = null,
            TargetFramework targetFramework = TargetFramework.NetStandard2_0,
            bool includePrerelease = false,
            bool recursive = false,
            IEnumerable<string>? sources = null)
        {
            OutputPath = outputPath ?? Directory.GetCurrentDirectory();
            TargetFramework = targetFramework;
            IncludePrerelease = includePrerelease;
            Recursive = recursive;
            Sources = sources;

            _packageMetadata = new PkgMetadata(_logger);
            _packageDownloader = new PkgDownloader(_logger);
        }

        public string OutputPath { get; }

        public TargetFramework TargetFramework { get; }

        public bool IncludePrerelease { get; }

        public bool Recursive { get; }

        public IEnumerable<string>? Sources { get; }

        public async Task DownloadPackageAsync(string packageName,
            string? version = null,
            bool extract = false,
            CancellationToken cancellationToken = default)
        {
            string downloadDir;
            string? extractDir;
            if (extract)
            {
                downloadDir = Path.Combine(Path.GetTempPath(), $"NuGetPackageDownloader.{DateTime.Now.Ticks}");
                extractDir = OutputPath;
            }
            else
            {
                downloadDir = OutputPath;
                extractDir = null;
            }

            NuGetManager manager = await NuGetManager.Create(TargetFramework, downloadDir, IncludePrerelease, Recursive, Sources, _logger);

            IReadOnlyList<PackageIdentity> packageIdentities = await _packageMetadata
                .GetPackageIdentitiesAsync(packageName, version, manager, cancellationToken);

            await _packageDownloader.DownloadPackagesAsync(
                packageIdentities.Select(pi => new PkgIdentity(pi, new HashSet<PackageIdentity>())),
                manager, extractDir, cancellationToken);
        }

        public async Task<IEnumerable<string>> GetPackageVersionsAsync(string packageName,
            CancellationToken cancellationToken = default)
        {
            NuGetManager manager = await NuGetManager.Create(TargetFramework, null, IncludePrerelease, Recursive, Sources, _logger);
            return await _packageMetadata.GetPackageVersionsAsync(packageName, manager, cancellationToken);
        }
    }
}
