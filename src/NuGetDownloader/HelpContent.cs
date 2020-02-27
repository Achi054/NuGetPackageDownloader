using System;
using System.Reflection;

namespace NuGetDownloader
{
    internal static class HelpContent
    {
        public static void RenderCommandHelp(string command)
        {
            Console.WriteLine($"Usage:  DownloadNuGet {command} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("-h|--help   Display help");
            Console.WriteLine("Command Options:");
            Console.WriteLine("  -n|--name:                  Name of the package you want to download");
            Console.WriteLine("  -f|--framework:             .Net target framework");
            Console.WriteLine("  -op|--output-path:          NuGet output path to extract the package(s)");
            Console.WriteLine("  -v|--version:               Search based on version of the package(s)");
            Console.WriteLine("  -ipr|--include-prerelease:  Search even on Pre-Releases of the package(s), true/false");
            Console.WriteLine();
        }

        public static void RenderInfo()
        {
            var versionString = Assembly.GetEntryAssembly()
                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                .InformationalVersion
                                .ToString();

            var summary = "DownloadNuGet is a cli tool to download and exrtact NuGet packages from nuget feed(s).\n" +
                          "The tool caters two needs, download NuGet package and the dependent NuGet(s)\n" +
                          "and extract NuGet package assemblies and dependent assemblies.";

            Console.WriteLine($"DownloadNuGet v{versionString}");
            Console.WriteLine(summary);
            Console.WriteLine();
        }

        public static void RenderHelp()
        {
            Console.WriteLine("Usage:  DownloadNuGet [commands][options]");
            Console.WriteLine();
            Console.WriteLine("Usage:  DownloadNuGet download [options]");
            Console.WriteLine("Usage:  DownloadNuGet extract [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("-h|--help    Display help");
            Console.WriteLine("-i|--info    Display tool information");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  download:   Downloads the NuGet package");
            Console.WriteLine("  extract:    Extracts the NuGet package assemblies and dependent assemblies");
            Console.WriteLine();
            Console.WriteLine("Command Options:");
            Console.WriteLine("  -name:                 Name of the package you want to download");
            Console.WriteLine("  -framework:            .Net target framework");
            Console.WriteLine("  -output:               NuGet output path to extract the package(s)");
            Console.WriteLine("  --version:             Search based on version of the package(s)");
            Console.WriteLine("  --include-prerelease:  Search even on Pre-Releases of the package(s), true/false");
            Console.WriteLine();
        }
    }
}
