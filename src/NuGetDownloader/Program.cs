using System;
using CommandLine;
using NugetPackageDownloader.Constants;

namespace NuGetDownloader
{
    public class Program
    {
        static void Main(string[] args)
        {
            var nuGetPackageDownloader = new NugetPackageDownloader.NuGetDownloader();

            var parseResult = new Parser(with => with.HelpWriter = null)
                .ParseArguments<DownloadCommand, ExtractCommand, VersionCommand>(args);

            parseResult
                .WithParsed<DownloadCommand>(opts =>
                {
                    if (Enum.TryParse<TargetFramework>(opts.Framework.Trim(), true, out var framework))
                    {
                        nuGetPackageDownloader.DownloadPackageAsync(
                                opts.Name.Trim(),
                                framework,
                                opts.OutputPath.Trim(),
                                downloaderOptions =>
                                {
                                    downloaderOptions.IncludePrerelease = opts.IncludePrerelease;
                                    downloaderOptions.Version = opts.Version?.Trim();
                                }).GetAwaiter().GetResult();
                    }
                })
                .WithParsed<ExtractCommand>(opts =>
                {
                    if (Enum.TryParse<TargetFramework>(opts.Framework.Trim(), true, out var framework))
                    {
                        nuGetPackageDownloader.DownloadAndExtractPackageAsync(
                                opts.Name.Trim(),
                                framework,
                                opts.OutputPath.Trim(),
                                downloaderOptions =>
                                {
                                    downloaderOptions.IncludePrerelease = opts.IncludePrerelease;
                                    downloaderOptions.Version = opts.Version?.Trim();
                                }).GetAwaiter().GetResult();
                    }
                })
                .WithParsed<VersionCommand>(opts =>
                {
                    nuGetPackageDownloader.GetPackageVersionsAsync(opts.Name.Trim()).GetAwaiter().GetResult();
                })
                .WithNotParsed(errs => HelpContent.DisplayHelp(parseResult, errs));
        }
    }
}
