using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NuGetPackageDownloader;

namespace NuGetDownloadTester
{
    internal static class Program
    {
        private const TargetFramework Framework = TargetFramework.NetCoreApp3_1;

        private const string DownloadDirectory = @"D:\Temp\Packages";

        private static readonly string[] Packages = new[]
        {
            "Swashbuckle.AspNetCore.Swagger",
            "Serilog",
            "Id3",
            "Moq",
            "Collections.NET",
            "IniFile.NET",
            "ContentProvider",
            "Ninject",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.SqlServer",
            //"Eurofins.Digital.AdoNet",
        };

        private const bool Extract = true;
        private const bool IncludePrerelease = false;
        private const bool Recursive = false;

        private static bool DownloadInParallel = false;

        private static async Task Main()
        {
            var sources = new[]
            {
                //"https://www.myget.org/F/eurofins-digital-online/auth/efcd5b4a-89d8-4491-9700-5b2ed013b2ce/api/v3/index.json",
                "https://api.nuget.org/v3/index.json",
            };

            if (!Directory.Exists(DownloadDirectory))
                Directory.CreateDirectory(DownloadDirectory);
            else
                EmptyDownloadDirectory();

            var metadata = new NuGetMetadata(sources);
            metadata.IncludePrerelease = false;
            foreach (string package in Packages)
            {
                Console.Write($"{package}: ");
                await foreach (string version in metadata.GetPackageVersionsAsync(package))
                    Console.Write(version + " ");
                Console.WriteLine();
            }

            var d = new NuGetPackageDownloader.NuGetDownloader(Framework,
                DownloadDirectory,
                includePrerelease: IncludePrerelease,
                recursive: Recursive,
                extract: Extract,
                sources: sources);

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                if (DownloadInParallel)
                    ParallelDownload(d, sw);
                else
                    await SynchronousDownload(d, sw);
            }
            finally
            {
                Console.WriteLine(sw.Elapsed);
            }
        }

        private static async Task SynchronousDownload(NuGetPackageDownloader.NuGetDownloader downloader, Stopwatch sw)
        {
            foreach (string package in Packages)
            {
                Console.WriteLine($"[{sw.Elapsed}] Downloading {package}...");
                await downloader.DownloadPackageAsync(package);
            }
        }

        private static void ParallelDownload(NuGetPackageDownloader.NuGetDownloader downloader, Stopwatch sw)
        {
            Parallel.ForEach(Packages, (package) =>
            {
                downloader.DownloadPackageAsync(package).GetAwaiter().GetResult();
                Console.WriteLine($"[{sw.Elapsed}] Downloading {package}...");
            });
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
