using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGetPackageDownloader.Internal;

namespace NuGetPackageDownloader
{
    public class NuGetDownloader
    {
        private readonly ILogger _logger = new NullLogger();
        private readonly PackageMetadata _packageMetadata;
        private readonly PackageDownloader _packageDownloader;

        public NuGetDownloader(string? outputPath = null,
            TargetFramework targetFramework = TargetFramework.NetStandard2_0,
            bool includePrerelease = false,
            IEnumerable<string>? sources = null)
        {
            OutputPath = outputPath ?? Directory.GetCurrentDirectory();
            TargetFramework = targetFramework;
            IncludePrerelease = includePrerelease;
            Sources = sources;

            _packageMetadata = new PackageMetadata(_logger);
            _packageDownloader = new PackageDownloader(_logger);
        }

        public string OutputPath { get; }

        public TargetFramework TargetFramework { get; }

        public bool IncludePrerelease { get; }

        public IEnumerable<string>? Sources { get; }

        public async Task DownloadPackageAsync(string packageName,
            string? version = null,
            bool extract = false,
            CancellationToken cancellationToken = default)
        {
            NuGetManager manager = await NuGetManager.Create(TargetFramework, OutputPath, IncludePrerelease, Sources, _logger);

            IEnumerable<PkgIdentity> packageIdentities = await _packageMetadata
                .GetPackageIdentitiesAsync(packageName, version, manager, cancellationToken);

            await _packageDownloader.DownloadPackagesAsync(
                packageIdentities, manager, cancellationToken);

            if (extract)
            {
                await _packageDownloader.ExtractPackageAssembliesAsync(
                    OutputPath, packageIdentities, manager, cancellationToken);
            }
        }

        public async Task<IEnumerable<string>> GetPackageVersionsAsync(string packageName,
            CancellationToken cancellationToken = default)
        {
            NuGetManager manager = await NuGetManager.Create(TargetFramework, null, IncludePrerelease, Sources, _logger);
            return await _packageMetadata.GetPackageVersionsAsync(packageName, manager, cancellationToken);
        }
    }
}
