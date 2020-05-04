using System;
using System.Collections.Generic;
using System.Linq;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NuGetPackageDownloader.Internal
{
    public abstract class NuGetAction
    {
        protected NuGetAction(IEnumerable<string>? sources)
        {
            ResourceProviders.AddRange(Repository.Provider.GetCoreV3());
            Settings = NuGet.Configuration.Settings.LoadDefaultSettings(null, null, new MachineWideSettings());
            SourceRepositories = GetSourceRepositories(sources).ToList();
        }

        protected NuGetAction(params string[] sources)
            : this((IEnumerable<string>)sources)
        {
        }

        public bool IncludePrerelease { get; set; }

        protected ILogger Logger { get; } = new NullLogger();

        protected List<Lazy<INuGetResourceProvider>> ResourceProviders { get; } = new List<Lazy<INuGetResourceProvider>>();

        protected ISettings Settings { get; }

        protected SourceCacheContext SourceCacheContext { get; } = new SourceCacheContext
        {
            NoCache = true,
            DirectDownload = true,
            RefreshMemoryCache = true,
        };

        protected IReadOnlyList<SourceRepository> SourceRepositories { get; }

        private IEnumerable<SourceRepository> GetSourceRepositories(IEnumerable<string>? sources)
        {

            if (sources != null && sources.Any())
            {
                foreach (string nugetFeed in sources)
                {
                    if (Uri.TryCreate(nugetFeed, UriKind.Absolute, out Uri uri) && !uri.IsFile)
                    {
                        SourceRepository? sourceRepo = GetSourceRepository(uri, ResourceProviders);
                        if (sourceRepo != null)
                            yield return sourceRepo;
                    }
                }
            }
            else
            {
                IEnumerable<AddItem>? disallowedPackageSources = Settings
                    .GetSection("disabledPackageSources")?
                    .Items
                    .Cast<AddItem>();

                IEnumerable<SourceItem> packageSources = Settings
                    .GetSection("packageSources")
                    .Items
                    .Cast<SourceItem>();

                foreach (SourceItem packageSource in packageSources)
                {
                    if (Uri.TryCreate(packageSource.Value, UriKind.Absolute, out Uri uri)
                        && !uri.IsFile
                        && (disallowedPackageSources is null || !disallowedPackageSources.Any(x => x.Key == packageSource.Key)))
                    {
                        SourceRepository? sourceRepo = GetSourceRepository(uri, ResourceProviders);
                        if (sourceRepo != null)
                            yield return sourceRepo;
                    }
                }
            }
        }

        private SourceRepository? GetSourceRepository(Uri feedUri, IList<Lazy<INuGetResourceProvider>> resourceProviders)
        {
            var sourceRepository = new SourceRepository(new PackageSource(feedUri.ToString()), resourceProviders);
            try
            {
                sourceRepository.GetResource<PackageMetadataResource>();
                return sourceRepository;
            }
            catch
            {
                return null;
            }
        }
    }
}
