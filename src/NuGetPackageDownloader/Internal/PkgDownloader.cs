using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectManagement;

namespace NuGetPackageDownloader.Internal
{
    internal sealed class PkgDownloader
    {
        private readonly ILogger _logger;

        internal PkgDownloader(ILogger logger)
        {
            _logger = logger;
        }

        internal async Task DownloadPackagesAsync(IEnumerable<PkgIdentity> identities,
            NuGetManager manager,
            string? extractDir,
            CancellationToken cancellationToken)
        {
            if (identities is null || !identities.Any())
                return;

            // OPTION 1
            IEnumerable<Task> tasks = identities.Select(async identity =>
            {
                bool packageAlreadyExists = manager.PackageManager.PackageExistsInPackagesFolder(
                    identity.Identity, PackageSaveMode.None);

                if (!packageAlreadyExists)
                    await manager.DownloadPackages(identity.Identity, cancellationToken);
            });
            await Task.WhenAll(tasks);

            // OPTION 2
            //var packageDownloadTasks = new List<Task>();

            //foreach (PkgIdentity identity in identities)
            //{
            //    packageDownloadTasks.Add(Task.Run(async () =>
            //    {
            //        bool packageAlreadyExists = manager.NuGetPackageManager.PackageExistsInPackagesFolder(
            //            identity.Identity, PackageSaveMode.None);

            //        if (!packageAlreadyExists)
            //            await manager.DownloadPackages(identity.Identity, cancellationToken);
            //    }, cancellationToken));
            //}

            //await Task.WhenAll(packageDownloadTasks);

            // OPTION 3: Parallel.ForEach
            //Parallel.ForEach(identities, identity =>
            //{
            //    bool packageAlreadyExists = manager.NuGetPackageManager.PackageExistsInPackagesFolder(
            //        identity.Identity, PackageSaveMode.None);

            //    if (!packageAlreadyExists)
            //        manager.DownloadPackages(identity.Identity, cancellationToken).GetAwaiter().GetResult();
            //});

            // OPTION 4: Synchronous
            //foreach (PkgIdentity identity in identities)
            //{
            //    bool packageAlreadyExists = manager.NuGetPackageManager.PackageExistsInPackagesFolder(
            //        identity.Identity, PackageSaveMode.None);

            //    if (!packageAlreadyExists)
            //        await manager.DownloadPackages(identity.Identity, cancellationToken);
            //}

            if (extractDir != null)
                ExtractPackageAssemblies(extractDir, identities, manager);
        }

        private void ExtractPackageAssemblies(string extractDir, IEnumerable<PkgIdentity> identities, NuGetManager manager)
        {
            var nugetProject = manager.Project as FolderNuGetProject;
            if (nugetProject is null)
                return;

            foreach (PkgIdentity identity in identities)
            {
                string packageFilePath = nugetProject.GetInstalledPackageFilePath(identity.Identity);
                if (string.IsNullOrWhiteSpace(packageFilePath))
                    continue;

                var archiveReader = new PackageArchiveReader(packageFilePath, null, null);
                FrameworkSpecificGroup? referenceGroup = GetMostCompatibleGroup(manager.Framework, archiveReader.GetReferenceItems());

                if (referenceGroup is null || referenceGroup.Items is null || !referenceGroup.Items.Any())
                    continue;

                if (!Directory.Exists(extractDir))
                    Directory.CreateDirectory(extractDir);

                string nugetPackagePath = nugetProject.GetInstalledPath(identity.Identity);
                Parallel.ForEach(referenceGroup.Items, x =>
                {
                    string sourceAssemblyPath = Path.Combine(nugetPackagePath, x);

                    string assemblyName = Path.GetFileName(sourceAssemblyPath);
                    string destinationAssemblyPath = Path.Combine(extractDir, assemblyName);

                    File.Copy(sourceAssemblyPath, destinationAssemblyPath, true);
                });
            }
        }

        private FrameworkSpecificGroup? GetMostCompatibleGroup(NuGetFramework projectTargetFramework,
            IEnumerable<FrameworkSpecificGroup> itemGroups)
        {
            NuGetFramework mostCompatibleFramework = new FrameworkReducer()
                .GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));

            if (mostCompatibleFramework is null)
                return null;

            FrameworkSpecificGroup mostCompatibleGroup = itemGroups.FirstOrDefault(i => i.TargetFramework.Equals(mostCompatibleFramework));
            return IsValid(mostCompatibleGroup) ? mostCompatibleGroup : null;
        }

        private bool IsValid(FrameworkSpecificGroup frameworkSpecificGroup)
        {
            return frameworkSpecificGroup != null
                ? frameworkSpecificGroup.HasEmptyFolder
                    || frameworkSpecificGroup.Items.Any()
                    || !frameworkSpecificGroup.TargetFramework.Equals(NuGetFramework.AnyFramework)
                : false;
        }
    }
}
