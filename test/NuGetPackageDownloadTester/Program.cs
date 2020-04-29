using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuGetDownloadTester
{
    internal static class Program
    {
        private static async Task Main()
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                var d = new NuGetPackageDownloader.NuGetDownloader(@"D:\Temp\Packages");
                foreach (var version in await d.GetPackageVersionsAsync("Swashbuckle.AspNetCore.Swagger"))
                    Console.WriteLine(version);
                await d.DownloadPackageAsync("Swashbuckle.AspNetCore.Swagger", extract: true);
            }
            finally
            {
                Console.WriteLine(sw.Elapsed);
            }
        }
    }
}
