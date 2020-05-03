using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NuGetPackageDownloader;

namespace NuGetDownloadTester
{
    internal static class Program
    {
        private const string DownloadDirectory = @"D:\Temp\Packages";

        private static async Task Main()
        {
            //const string packageName = "Swashbuckle.AspNetCore.Swagger";
            //const string packageName = "Serilog";
            //const string packageName = "ConsoleFx";
            const string packageName = "Eurofins.Digital.AdoNet";

            if (!Directory.Exists(DownloadDirectory))
                Directory.CreateDirectory(DownloadDirectory);
            else
                EmptyDownloadDirectory();

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                var sources = new[] 
                {
                    "https://www.myget.org/F/eurofins-digital-online/auth/efcd5b4a-89d8-4491-9700-5b2ed013b2ce/api/v3/index.json",
                    "https://api.nuget.org/v3/index.json",
                };
                //string[] sources = null;

                var d = new NuGetPackageDownloader.NuGetDownloader(DownloadDirectory, TargetFramework.NetCoreApp3_1, includePrerelease: false, sources: sources);
                foreach (var version in await d.GetPackageVersionsAsync(packageName))
                    Console.WriteLine(version);
                await d.DownloadPackageAsync(packageName, extract: true);
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
