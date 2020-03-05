using System;
using System.Threading.Tasks;
using CommandLine;
using NugetPackageDownloader.Constants;

namespace NuGetDownloader
{
    public class Program
    {
        static Task Main(string[] args)
        {
            var nuGetPackageDownloader = new NugetPackageDownloader.NuGetDownloader();

            var parseResult = new Parser(with => with.HelpWriter = null)
                .ParseArguments<DownloadCommand, ExtractCommand, ShowCommand>(args);

            parseResult
                .WithParsed<DownloadCommand>(async opts =>
                {
                    if (Enum.TryParse<TargetFramework>(opts.Framework, true, out var framework))
                    {
                        await nuGetPackageDownloader.DownloadPackageAsync(
                                opts.Name,
                                framework,
                                opts.OutputPath,
                                downloaderOptions =>
                                {
                                    downloaderOptions.IncludePrerelease = opts.IncludePrerelease;
                                    downloaderOptions.Version = opts.Version;
                                });
                    }
                })
                .WithParsed<ExtractCommand>(async opts =>
                {
                    if (Enum.TryParse<TargetFramework>(opts.Framework, true, out var framework))
                    {
                        await nuGetPackageDownloader.DownloadAndExtractPackageAsync(
                                opts.Name,
                                framework,
                                opts.OutputPath,
                                downloaderOptions =>
                                {
                                    downloaderOptions.IncludePrerelease = opts.IncludePrerelease;
                                    downloaderOptions.Version = opts.Version;
                                });
                    }
                })
                .WithParsed<ShowCommand>(async opts => await nuGetPackageDownloader.GetPackageVersionsAsync(opts.Name))
                .WithNotParsed(errs => HelpContent.DisplayHelp(parseResult, errs));

            return Task.CompletedTask;
        }
    }
}
