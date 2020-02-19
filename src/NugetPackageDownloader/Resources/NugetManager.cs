using System;
using System.Collections.Generic;

using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NugetPackageDownloader.Resources
{
    public class NugetManager
    {
        // Read from source nuget.config
        private const string nuGetPackageFeed = "https://api.nuget.org/v3/index.json";

        public NugetManager()
        {
            SourceCacheContext = new SourceCacheContext
            {
                NoCache = true,
                DirectDownload = true,
                RefreshMemoryCache = true,
            };

            ResourceProviders = new List<Lazy<INuGetResourceProvider>>();

            // Add API support for v3, include v2 support if needed
            ResourceProviders.AddRange(Repository.Provider.GetCoreV3());

            // Setup package feeds, this could be a collection of multiple feeds
            PackageSource = new PackageSource(nuGetPackageFeed);

            SourceRepository = new SourceRepository(PackageSource, ResourceProviders);
        }

        public List<Lazy<INuGetResourceProvider>> ResourceProviders { get; }

        public PackageSource PackageSource { get; }

        public SourceRepository SourceRepository { get; }

        public SourceCacheContext SourceCacheContext { get; }
    }
}
