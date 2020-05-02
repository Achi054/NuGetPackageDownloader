using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NuGetDownloadTester
{
    internal static class Program
    {
        private const string DownloadDirectory = @"D:\Temp\Packages";

        private static async Task Main()
        {
            //const string packageName = "Swashbuckle.AspNetCore.Swagger";
            //const string packageName = "Serilog";
            const string packageName = "ConsoleFx";

            if (!Directory.Exists(DownloadDirectory))
                Directory.CreateDirectory(DownloadDirectory);
            else
                EmptyDownloadDirectory();

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                var d = new NuGetPackageDownloader.NuGetDownloader(DownloadDirectory, includePrerelease: false);
                foreach (var version in await d.GetPackageVersionsAsync(packageName))
                    Console.WriteLine(version);
                await d.DownloadPackageAsync(packageName, extract: false);
            }
            finally
            {
                Console.WriteLine(sw.Elapsed);
            }
        }

        private static void EmptyDownloadDirectory()
        {
            string[] dirs = Directory.GetDirectories(DownloadDirectory, "*", SearchOption.TopDirectoryOnly);
            foreach (string dir in dirs)
                Directory.Delete(dir, recursive: true);

            string[] files = Directory.GetFiles(DownloadDirectory, "*", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
                File.Delete(file);
        }
    }
}
