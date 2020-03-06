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

    [Verb("versions", HelpText = "Display available package versions in NuGet respository(s)")]
    internal class VersionCommand : BaseOptions
    {
        [Usage(ApplicationAlias = "versions")]
        public static IEnumerable<Example> ExtractExamples
        {
            get => new[] {
                new Example("Display available package versions in NuGet respository(s)",
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
                    new BaseOptions
                    {
                        Name = "<name>"
                    }),
            };
        }
    }

    internal class Options : BaseOptions
    {
        [Option('f', "framework", Required = true, HelpText = ".Net target framework")]
        public string Framework { get; set; }

        [Option('o', "output", Required = true, HelpText = "NuGet output path to download/extract the package(s)")]
        public string OutputPath { get; set; }

        [Option('v', "version", Required = false, HelpText = "Search based on version of the package(s)")]
        public string Version { get; set; }

        [Option('p', "include-prerelease", Required = false, HelpText = "Search even on Pre-Releases of the package(s)")]
        public bool IncludePrerelease { get; set; }
    }

    internal class BaseOptions
    {
        [Option('n', "name", Required = true, HelpText = "Name of the package")]
        public string Name { get; set; }
    }
}
