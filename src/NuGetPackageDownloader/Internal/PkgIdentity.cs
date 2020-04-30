using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NuGetPackageDownloader.Internal
{
    [DebuggerDisplay("{Name} ({Version})")]
    internal sealed class PkgIdentity
    {
        internal PkgIdentity(PackageIdentity identity,
            ISet<PackageIdentity> dependentPackageIdentities)
        {
            Identity = identity;
            DependentPackageIdentities = dependentPackageIdentities;
        }

        internal string Name => Identity.Id;

        internal NuGetVersion Version => Identity.Version;

        internal PackageIdentity Identity { get; }

        internal ISet<PackageIdentity> DependentPackageIdentities { get; }
    }

    internal sealed class PkgIdentities : Collection<PkgIdentity>
    {
        protected override void InsertItem(int index, PkgIdentity item)
        {
            if (!IsDuplicate(item, -1))
                base.InsertItem(index, item);
        }

        protected override void SetItem(int index, PkgIdentity item)
        {
            if (!IsDuplicate(item, index))
                base.SetItem(index, item);
        }

        private bool IsDuplicate(PkgIdentity identity, int index)
        {
            for (int i = 0; i < Count; i++)
            {
                if (i == index)
                    continue;
                if (this[i].Name == identity.Name)
                    return true;
            }

            return false;
        }
    }
}
