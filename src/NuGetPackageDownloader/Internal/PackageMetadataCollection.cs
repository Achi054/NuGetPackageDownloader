using System.Collections.ObjectModel;

using NuGet.Protocol.Core.Types;

namespace NuGetPackageDownloader.Internal
{
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
