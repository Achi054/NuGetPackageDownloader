using System.Collections.Generic;
using System.Diagnostics;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NuGetPackageDownloader.Internal
{
    [DebuggerDisplay("{Name} ({Version})")]
    internal sealed class PkgIdentity
    {
        internal PkgIdentity(string name,
            NuGetVersion version,
            PackageIdentity identity,
            ISet<PackageIdentity> dependentPackageIdentities)
        {
            Name = name;
            Version = version;
            Identity = identity;
            DependentPackageIdentities = dependentPackageIdentities;
        }

        internal string Name { get; }

        internal NuGetVersion Version { get; }

        internal PackageIdentity Identity { get; }

        internal ISet<PackageIdentity> DependentPackageIdentities { get; }
    }
}
