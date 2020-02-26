using System.Threading.Tasks;

using NugetPackageDownloader;
using NugetPackageDownloader.Constants;

namespace NuGetDownloadTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const string NUGET_PACKAGE_NAME = "Serilog";
            const string VERSION = "2.9.1-dev-01154";
            const string PATH = @"C:\TigerBox\POC\NugetPackageDownloader\bin";

            await new NuGetDownloader().DownloadAndExtractPackage(
                NUGET_PACKAGE_NAME,
                TargetFramework.NETSTANDARD2_1,
                PATH,
                downloaderOptions =>
                {
                    downloaderOptions.IncludePrerelease = true;
                    downloaderOptions.Version = VERSION;
                });
        }
    }
}
