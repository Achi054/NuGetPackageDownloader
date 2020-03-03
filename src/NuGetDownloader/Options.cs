using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace NuGetDownloader
{
    [Verb("download", HelpText = "Download NuGet package(s)")]
    internal class DownloadCommand : Options
    {
        [Usage(ApplicationAlias = "download")]
        public static IEnumerable<Example> DownloadExamples
        {
            get => new[] {
                new Example("Download NuGet package(s)",
                    new[] {
                        new UnParserSettings
                        {
                            PreferShortName = true,
                            UseEqualToken = true,
                        },
                        new UnParserSettings
                        {
                            PreferShortName = false,
                            UseEqualToken = false,
                        },
                    },
                    new Options
                    {
                        Name = "<name>",
                        Framework = "<framework>",
                        OutputPath = "<output-path>"
                    }),
            };
        }
    }

    [Verb("extract", HelpText = "Download and Extract NuGet package and dependent assemblies")]
    internal class ExtractCommand : Options
    {
        public Options Options { get; set; }

        [Usage(ApplicationAlias = "extract")]
        public static IEnumerable<Example> ExtractExamples
        {
            get => new[] {
                new Example("Download and Extract NuGet package and dependent assemblies",
                    new[] {
                        new UnParserSettings
                        {
                            PreferShortName = true,
                            UseEqualToken = true,
                        },
                        new UnParserSettings
                        {
                            PreferShortName = false,
                            UseEqualToken = false,
                        },
                    },
                    new Options
                    {
                        Name = "<name>",
                        Framework = "<framework>",
                        OutputPath = "<output>"
                    }),
            };
        }
    }

    internal class Options
    {
        [Option('n', "name", Required = true, HelpText = "Name of the package you want to download")]
        public string Name { get; set; }

        [Option('f', "framework", Required = true, HelpText = ".Net target framework")]
        public string Framework { get; set; }

        [Option('o', "output", Required = true, HelpText = "NuGet output path to download/extract the package(s)")]
        public string OutputPath { get; set; }

        [Option('v', "version", Required = false, HelpText = "Search based on version of the package(s)")]
        public string Version { get; set; }

        [Option('p', "include-prerelease", Required = false, HelpText = "Search even on Pre-Releases of the package(s), true/false")]
        public bool IncludePrerelease { get; set; }
    }
}
