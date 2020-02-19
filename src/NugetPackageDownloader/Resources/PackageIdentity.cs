using System.Collections.Generic;

using NuGet.Versioning;
using NuGetCore = NuGet.Packaging.Core;

namespace NugetPackageDownloader.Resources
{
    public class PackageIdentity
    {
        public PackageIdentity(
            string name,
            NuGetVersion version,
            NuGetCore.PackageIdentity identity,
            HashSet<NuGetCore.PackageIdentity> dependentPackageIdentities)
            => (Name, Version, Identity, DependentPackageIdentities) = (name, version, identity, dependentPackageIdentities);

        public string Name { get; }
        public NuGetVersion Version { get; }
        public NuGetCore.PackageIdentity Identity { get; }
        public ISet<NuGetCore.PackageIdentity> DependentPackageIdentities { get; }
    }
}
