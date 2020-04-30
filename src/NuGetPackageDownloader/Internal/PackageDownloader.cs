using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;

namespace NuGetPackageDownloader.Internal
{
    internal sealed class PackageDownloader
    {
        private readonly ILogger _logger;

        internal PackageDownloader(ILogger logger)
        {
            _logger = logger;
        }

        internal async Task DownloadPackagesAsync(IEnumerable<PkgIdentity> identities,
            NuGetManager manager,
            CancellationToken cancellationToken = default)
        {
            if (identities is null || !identities.Any())
                return;

            var packageDownloadTasks = new List<Task>();

            foreach (PkgIdentity identity in identities)
            {
                packageDownloadTasks.Add(Task.Run(async () =>
                {
                    if (identity != null)
                    {
                        bool packageAlreadyExists = manager.NuGetPackageManager.PackageExistsInPackagesFolder(
                            identity.Identity, PackageSaveMode.None);

                        if (!packageAlreadyExists)
                            await manager.DownloadPackages(identity.Identity, cancellationToken);
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(packageDownloadTasks);
        }

        internal async Task ExtractPackageAssembliesAsync(string outputPath,
            IEnumerable<PkgIdentity> identities,
            NuGetManager manager,
            CancellationToken cancellationToken = default)
        {
            if (identities is null || !identities.Any())
                return;

            foreach (PkgIdentity identity in identities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await CopyNuGetAssembliesAsync(outputPath, manager, identity.Identity, cancellationToken);

                var copyTasks = identity.DependentPackageIdentities
                    .Select(dependentPackageIdentity => CopyNuGetAssembliesAsync(outputPath, manager, dependentPackageIdentity, cancellationToken));

                await Task.WhenAll(copyTasks);
            }
        }

        private Task CopyNuGetAssembliesAsync(string outputPath,
            NuGetManager manager,
            PackageIdentity packageIdentity,
            CancellationToken cancellationToken = default)
        {
            if (manager.NuGetProject is FolderNuGetProject nuGetProject)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packageFilePath = nuGetProject.GetInstalledPackageFilePath(packageIdentity);
                if (!string.IsNullOrWhiteSpace(packageFilePath))
                {
                    var archiveReader = new PackageArchiveReader(packageFilePath, null, null);
                    var referenceGroup = GetMostCompatibleGroup(manager.NuGetFramework, archiveReader.GetReferenceItems());

                    if (referenceGroup != null && referenceGroup.Items != null && referenceGroup.Items.Any())
                    {
                        if (!Directory.Exists(outputPath))
                            Directory.CreateDirectory(outputPath);

                        _logger.LogInformation($"Output path: {outputPath}\n");

                        var nuGetPackagePath = nuGetProject.GetInstalledPath(packageIdentity);
                        referenceGroup.Items.ToList().ForEach(x =>
                        {
                            var sourceAssemblyPath = $@"{nuGetPackagePath}\{x}".Replace('/', '\\');

                            string assemblyName = Path.GetFileName(sourceAssemblyPath);
                            string destinationAssemblyName = Path.Combine(outputPath, assemblyName);

                            File.Copy(sourceAssemblyPath, destinationAssemblyName, true);
                        });
                    }
                }
            }

            return Task.CompletedTask;
        }

        private FrameworkSpecificGroup? GetMostCompatibleGroup(NuGetFramework projectTargetFramework,
            IEnumerable<FrameworkSpecificGroup> itemGroups)
        {
            var mostCompatibleFramework = new FrameworkReducer()
                .GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));

            if (mostCompatibleFramework != null)
            {
                var mostCompatibleGroup = itemGroups.FirstOrDefault(i => i.TargetFramework.Equals(mostCompatibleFramework));

                if (IsValid(mostCompatibleGroup))
                    return mostCompatibleGroup;
            }

            return null;
        }

        private bool IsValid(FrameworkSpecificGroup frameworkSpecificGroup) => frameworkSpecificGroup != null
                ? frameworkSpecificGroup.HasEmptyFolder
                        || frameworkSpecificGroup.Items.Any()
                        || !frameworkSpecificGroup.TargetFramework.Equals(NuGetFramework.AnyFramework)
                : false;
    }
}
