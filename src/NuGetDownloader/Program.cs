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
            args = @"download -n Serilog -o C:\TigerBox\POC\NuGetPackageDownloader\bin -f NETSTANDARD2_0".Split(' ');
            var nuGetPackageDownloader = new NugetPackageDownloader.NuGetDownloader();

            var parseResult = new Parser(with => with.HelpWriter = null)
                .ParseArguments<DownloadCommand, ExtractCommand, VersionCommand>(args);

            parseResult
                .WithParsed<DownloadCommand>(async opts =>
                {
                    if (Enum.TryParse<TargetFramework>(opts.Framework.Trim(), true, out var framework))
                    {
                        await nuGetPackageDownloader.DownloadPackageAsync(
                                opts.Name.Trim(),
                                framework,
                                opts.OutputPath.Trim(),
                                downloaderOptions =>
                                {
                                    downloaderOptions.IncludePrerelease = opts.IncludePrerelease;
                                    downloaderOptions.Version = opts.Version?.Trim();
                                });
                    }
                })
                .WithParsed<ExtractCommand>(async opts =>
                {
                    if (Enum.TryParse<TargetFramework>(opts.Framework.Trim(), true, out var framework))
                    {
                        await nuGetPackageDownloader.DownloadAndExtractPackageAsync(
                                opts.Name.Trim(),
                                framework,
                                opts.OutputPath.Trim(),
                                downloaderOptions =>
                                {
                                    downloaderOptions.IncludePrerelease = opts.IncludePrerelease;
                                    downloaderOptions.Version = opts.Version?.Trim();
                                });
                    }
                })
                .WithParsed<VersionCommand>(async opts => await nuGetPackageDownloader.GetPackageVersionsAsync(opts.Name.Trim()))
                .WithNotParsed(errs => HelpContent.DisplayHelp(parseResult, errs));

            return Task.CompletedTask;
        }
    }
}
