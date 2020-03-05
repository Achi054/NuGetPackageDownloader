using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace NuGetDownloader
{
    internal class HelpContent
    {
        static List<string> DownloadHelp = new[] {
            "Usage:  DownloadNuGet download [options]",
            Environment.NewLine,
            "Options:",
            "  help, --help                Display help",
            Environment.NewLine,
            "Command Options:",
            "  -n, --name:                 Name of the package you want to download",
            "  -f, --framework:            .Net target framework",
            "  -o, --output-path:          NuGet output path to extract the package(s)",
            "  -v, --version:              Search based on version of the package(s)",
            "  -p, --include-prerelease:   Search even on Pre-Releases of the package(s)"}.ToList();

        static List<string> ExtractHelp = new[] {
            "Usage:  DownloadNuGet extract [options]",
            Environment.NewLine,
            "Options:",
            "  help, --help                Display help",
            Environment.NewLine,
            "Command Options:",
            "  -n, --name:                 Name of the package you want to download",
            "  -f, --framework:            .Net target framework",
            "  -o, --output-path:          NuGet output path to extract the package(s)",
            "  -v, --version:              Search based on version of the package(s)",
            "  -p, --include-prerelease:   Search even on Pre-Releases of the package(s)" }.ToList();

        static List<string> ShowHelp = new[] {
            "Usage:  DownloadNuGet display-versions [options]",
            Environment.NewLine,
            "Options:",
            "  help, --help                Display help",
            Environment.NewLine,
            "Command Options:",
            "  -n, --name:                 Name of the package" }.ToList();

        static List<string> Help = new[] {
            "  Usage:  DownloadNuGet [commands][options]",
            "      Usage:  DownloadNuGet download [options]",
            "      Usage:  DownloadNuGet extract [options]",
            "      Usage:  DownloadNuGet display-versions [options]",
            Environment.NewLine,
            "  Options:",
            "     help, --help    Display help",
            Environment.NewLine,
            "  Commands:",
            "    download:   Downloads the NuGet package",
            "    extract:    Extracts the NuGet package assemblies and dependent assemblies",
            "    display-versions:    Display available package versions in NuGet respository(s)",
            Environment.NewLine,
            "  Command Options:",
            "      -n, --name:                Name of the package",
            "      -f, --framework:           .Net target framework",
            "      -o, --output:              NuGet output path to extract the package(s)",
            "      -v, --version:             Search based on version of the package(s)",
            "      -p, --include-prerelease:  Search even on Pre-Releases of the package(s)",
            }.ToList();

        public static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var versionString = Assembly.GetEntryAssembly()
                                   .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                   .InformationalVersion.ToString();

            var copyrightstring = Assembly.GetEntryAssembly()
                                .GetCustomAttribute<AssemblyCopyrightAttribute>()
                                .Copyright.ToString();

            HelpText helpText = HelpText.AutoBuild(result, h =>
            {
                h.Heading = $"NuGetDownloader v{versionString}";
                h.Copyright = copyrightstring;
                h.AutoHelp = h.AutoVersion = h.AdditionalNewLineAfterOption = false;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);

            if (result is DownloadCommand)
                helpText.AddPostOptionsLines(DownloadHelp);
            else if (result is ExtractCommand)
                helpText.AddPostOptionsLines(ExtractHelp);
            else if (result is VersionCommand)
                helpText.AddPostOptionsLines(ShowHelp);
            else
                helpText.AddPostOptionsLines(Help);

            Console.WriteLine(helpText);
        }
    }
}
