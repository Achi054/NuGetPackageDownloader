using System;

using CommandLine;

using NuGetPackageDownloader;

namespace NuGetDownloader
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var downloader = new NuGetPackageDownloader.NuGetDownloader();

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
                        await downloader.DownloadPackageAsync(opts.Name.Trim(), extract: true);
                })
                .WithParsed<VersionCommand>(async opts =>
                {
                    await downloader.GetPackageVersionsAsync(opts.Name.Trim());
                })
                .WithNotParsed(errs => HelpContent.DisplayHelp(parseResult, errs));
        }
    }
}
