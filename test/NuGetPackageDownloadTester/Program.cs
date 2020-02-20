using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NugetPackageDownloader;
using NugetPackageDownloader.Helpers;
using NugetPackageDownloader.Logging;
using NugetPackageDownloader.Resources;
using NugetPackageDownloader.Resources.Downloader;
using NugetPackageDownloader.Resources.Metadata;

namespace NuGetDownloadTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const string NUGET_PACKAGE_NAME = "EntityFramework";
            const string VERSION = "6.4.0";
            const string PATH = @"C:\TigerBox\POC\NugetPackageDownloader\bin";

            //Setup Dependency Container
            var serviceProvider = new ServiceCollection()
                                        .AddLogging(configure => configure.AddConsole())
                                        .AddSingleton<NuGet.Common.ILogger, NuGetLogger>()
                                        .AddSingleton<NuGetManager>()
                                        .AddSingleton<IPackageMetadata, PackageMetadata>()
                                        .AddSingleton<IPackageDownloader, PackageDownloader>()
                                        .AddSingleton<INuGetDownloader, NuGetDownloader>()
                                        .BuildServiceProvider();

            var nuGetdownloader = serviceProvider.GetRequiredService<INuGetDownloader>();
            await nuGetdownloader.DownloadPackage(
                NUGET_PACKAGE_NAME,
                TargetFramework.NETSTANDARD2_1,
                downloaderOptions =>
                {
                    downloaderOptions.IncludePrerelease = false;
                    downloaderOptions.Version = VERSION;
                    downloaderOptions.OutputPath = PATH;
                });
        }
    }
}
