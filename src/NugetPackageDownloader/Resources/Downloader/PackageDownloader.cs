using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NuGetCore = NuGet.Packaging.Core;

namespace NugetPackageDownloader.Resources.Downloader
{
    internal class PackageDownloader : IPackageDownloader
    {
        private readonly ILogger _logger;

        internal PackageDownloader(ILogger logger = default) => _logger = logger;

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

                                    _logger?.LogInformation($"Download of package {packageIdentity.Name}.{packageIdentity.Version.ToString()} is complete");
                                }
                            }
                        }));
                    });

                    Task.WaitAll(packageToDownloadTask.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Download packages failed due to the below errors:\nMessage:{ex.Message}\nStackTrace:{ex.StackTrace}");
                throw;
            }

            return Task.CompletedTask;
        }

        public async Task ExtractPackageAssemblies(
            string outputPath,
            IEnumerable<PackageIdentity> packageIdentities,
            NuGetManager nuGetManager,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (packageIdentities != null && packageIdentities.Any())
                {
                    foreach (var packageIdentity in packageIdentities)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await CopyNuGetAssemblies(outputPath, nuGetManager, packageIdentity.Identity, cancellationToken);

                        var copyTasks = new List<Task>();

                        packageIdentity.DependentPackageIdentities.ToList()
                            .ForEach(dependentPackageIdentity =>
                            {
                                copyTasks.Add(CopyNuGetAssemblies(outputPath, nuGetManager, dependentPackageIdentity, cancellationToken));
                            });

                        Task.WaitAll(copyTasks.ToArray());
                    }
                }

                _logger?.LogInformation("Extracting package assets complete");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Extracting package assets failed due to the below errors:\nMessage:{ex.Message}\nStackTrace:{ex.StackTrace}");
                throw;
            }
        }

        private Task CopyNuGetAssemblies(
            string outputPath,
            NuGetManager nuGetManager,
            NuGetCore.PackageIdentity packageIdentity,
            CancellationToken cancellationToken = default)
        {
            if (nuGetManager.NuGetProject is FolderNuGetProject nuGetProject)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packageFilePath = nuGetProject.GetInstalledPackageFilePath(packageIdentity);
                if (!string.IsNullOrWhiteSpace(packageFilePath))
                {
                    var archiveReader = new PackageArchiveReader(packageFilePath, null, null);
                    var referenceGroup = GetMostCompatibleGroup(nuGetManager.NuGetFramework, archiveReader.GetReferenceItems());

                    if (referenceGroup != null && referenceGroup.Items != null && referenceGroup.Items.Any())
                    {
                        if (!Directory.Exists(outputPath))
                        {
                            Directory.CreateDirectory(outputPath);
                        }

                        _logger?.LogInformation($"Output path: {outputPath}\n");

                        var nuGetPackagePath = nuGetProject.GetInstalledPath(packageIdentity);
                        referenceGroup.Items.ToList().ForEach(x =>
                        {
                            var sourceAssemblyPath = $@"{nuGetPackagePath}\{x}".Replace('/', '\\');

                            var assemblyName = Path.GetFileName(sourceAssemblyPath);
                            var destinationAssemblyName = Path.Combine(outputPath, assemblyName);

                            File.Copy(sourceAssemblyPath, destinationAssemblyName, true);
                        });
                    }
                }
            }

            return Task.CompletedTask;
        }

        private FrameworkSpecificGroup GetMostCompatibleGroup(
            NuGetFramework projectTargetFramework,
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
