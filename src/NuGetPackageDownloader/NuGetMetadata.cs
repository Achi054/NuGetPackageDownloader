using System.Collections.Generic;
using System.Linq;
using System.Threading;

using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetPackageDownloader.Internal;

#if NETSTANDARD2_0
using System.Threading.Tasks;
#endif

#if NETSTANDARD2_1
using System.Runtime.CompilerServices;
#endif

namespace NuGetPackageDownloader
{
    public class NuGetMetadata : NuGetAction
    {
        public NuGetMetadata(params string[] sources)
            : base(sources)
        {
        }

        public NuGetMetadata(IEnumerable<string>? sources)
            : base(sources)
        {
        }

#if NETSTANDARD2_0
        public async Task<IEnumerable<string>> GetPackageVersionsAsync(string packageName,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<string> result = Enumerable.Empty<string>();

            foreach (SourceRepository sourceRepository in SourceRepositories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FindPackageByIdResource resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

                IEnumerable<NuGetVersion> versions = (await resource.GetAllVersionsAsync(packageName,
                        SourceCacheContext,
                        Logger,
                        cancellationToken))
                    .Where(ver => !ver.IsPrerelease || IncludePrerelease);

                result = result.Concat(versions.Select(v => v.ToNormalizedString()));
            }

            return result;
        }
#endif

#if NETSTANDARD2_1
        public async IAsyncEnumerable<string> GetPackageVersionsAsync(string packageName,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (SourceRepository sourceRepository in SourceRepositories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FindPackageByIdResource resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

                IEnumerable<NuGetVersion> versions = (await resource.GetAllVersionsAsync(packageName,
                        SourceCacheContext,
                        Logger,
                        cancellationToken))
                    .Where(ver => !ver.IsPrerelease || IncludePrerelease);

                foreach (NuGetVersion version in versions)
                    yield return version.ToNormalizedString();
            }
        }
#endif
    }
}
