using System;

using CommandLine;

using NuGetPackageDownloader;

namespace NuGetDownloader
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var downloader = new NuGetPackageDownloader.NuGetDownloader(TargetFramework.NetCoreApp3_1);

            ParserResult<object> parseResult = new Parser(with => with.HelpWriter = null)
                .ParseArguments<DownloadCommand, ExtractCommand, VersionCommand>(args);

            parseResult
                .WithParsed<DownloadCommand>(async opts =>
                {
                    if (Enum.TryParse(opts.Framework.Trim(), true, out TargetFramework framework))
                        await downloader.DownloadPackageAsync(opts.Name.Trim());
                })
                .WithParsed<ExtractCommand>(async opts =>
                {
                    if (Enum.TryParse(opts.Framework.Trim(), true, out TargetFramework framework))
                        await downloader.DownloadPackageAsync(opts.Name.Trim());
                })
                .WithParsed<VersionCommand>(async opts =>
                {
                    var metadata = new NuGetMetadata();
                    var versions = await metadata.GetPackageVersionsAsync(opts.Name.Trim());
                    foreach (string version in versions)
                        Console.WriteLine(version);
                })
                .WithNotParsed(errs => HelpContent.DisplayHelp(parseResult, errs));
        }
    }
}
