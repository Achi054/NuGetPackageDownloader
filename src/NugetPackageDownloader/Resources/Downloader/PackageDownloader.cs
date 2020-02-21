using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Packaging;

namespace NugetPackageDownloader.Resources.Downloader
{
    public class PackageDownloader : IPackageDownloader
    {
        private readonly ILogger _logger;

        public PackageDownloader(ILogger logger) => _logger = logger;

        public Task DownloadPackages(
            IEnumerable<PackageIdentity> packageIdentities,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (packageIdentities != null && packageIdentities.Any())
                {
                    var packageToDownloadTask = new List<Task>();

                    packageIdentities.ToList().ForEach(packageIdentity =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        packageToDownloadTask.Add(Task.Run(async () =>
                        {
                            if (packageIdentity != null)
                            {
                                bool packageAlreadyExists = nuGetManager.NuGetPackageManager.PackageExistsInPackagesFolder(
                                    packageIdentity.Identity, PackageSaveMode.None);

                                if (!packageAlreadyExists)
                                {
                                    await nuGetManager.DownloadPackages(packageIdentity.Identity, cancellationToken);

                                    _logger.LogInformation($"Download of package {packageIdentity.Name}.{packageIdentity.Version.ToString()} is complete");
                                }
                            }
                        }));
                    });

                    Task.WaitAll(packageToDownloadTask.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Download packages failed due to the below errors:\nMessage:{ex.Message}\nStackTrace:{ex.StackTrace}");
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
