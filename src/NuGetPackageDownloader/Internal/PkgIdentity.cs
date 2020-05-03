using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

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
                if (this[i].Identity.Id == identity.Identity.Id)
                    return true;
            }

            return false;
        }
    }

    internal sealed class PackageMetadataCollection : Collection<IPackageSearchMetadata>
    {
        protected override void InsertItem(int index, IPackageSearchMetadata item)
        {
            if (!IsDuplicate(item, -1))
                base.InsertItem(index, item);
        }

        protected override void SetItem(int index, IPackageSearchMetadata item)
        {
            if (!IsDuplicate(item, index))
                base.SetItem(index, item);
        }

        private bool IsDuplicate(IPackageSearchMetadata metadata, int index)
        {
            for (int i = 0; i < Count; i++)
            {
                if (i == index)
                    continue;
                if (this[i].Identity.Id == metadata.Identity.Id)
                    return true;
            }

            return false;
        }
    }
}
