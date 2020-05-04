# NuGet Package Downloader

![NuGet Package Downloader](./src/NuGetPackageDownloader/Assets/Titan.png)

master | dev | nuget
-------|-----|------
[![Build status](https://ci.appveyor.com/api/projects/status/0f4u7344b392pfsv/branch/master?svg=true)](https://ci.appveyor.com/project/Achi054/nugetpackagedownloader/branch/master) | [![Build status](https://ci.appveyor.com/api/projects/status/0f4u7344b392pfsv/branch/dev?svg=true)](https://ci.appveyor.com/project/Achi054/nugetpackagedownloader/branch/dev) | [![NuGet Version](http://img.shields.io/nuget/v/NuGetPackageDownloader.svg?style=flat)](https://www.nuget.org/packages/NuGetPackageDownloader/)

NuGet package downloader is a .NET Standard 2.0/2.1 library to ease the download of NuGet packages from NuGet feeds.

Most of the application these days are build as tools or plugins, the need of a tool to download and/or extract needful NuGet packages and its dependencies on demand has surfaced.

The library exposes two classes - `NuGetMetadata` and `NuGetDownloader`.

## `NuGetMetadata` class
The `NuGetMetadata` class exposes useful information from NuGet packages.

```cs
var metadata = new NuGetMetadata();

// Get the latest version of a package
string latestVersion = await metadata.GetLatestPackageVersionAsync("Serilog");

// Get all versions of a package (netstandard2.0 version)
IEnumerable<string> versions = await metadata.GetPackageVersionsAsync("Serilog");

// The netstandard2.1 version returns an IAsyncEnumerable
await foreach (string version in metadata.GetPackageVersionsAsync("Serilog"))
    ...
```

## `NuGetDownloader` class
The `NuGetDownloader` class allows you to download a package (specific or latest version) to disk. It can optionally extract the libraries from the package and save it to a location on disk.

```cs
var downloader = new NuGetDownloader(
    // The target framework to choose the most suitable libraries to extract
    TargetFramework.NetCoreApp3_1,
    
    // Location on disk to download the packages or extract the libraries to (Optional)
    outputDir: Directory.GetCurrentDirectory(),
    
    // Whether to include prerelease versions of the package (Optional)
    includePrerelease: false,
    
    // Whether to recursively download all dependent packages (Optional)
    recursive: false,
    
    // Whether the extract the libraries from the packages (Optional)
    extract: true,
    
    // One or more NuGet sources to search for the packages (Optional)
    // If not specified, defaults to the global NuGet.config sources
    sources: null);

// Download the latest version of a package
await downloader.DownloadPackageAsync("Serilog");

// Download a specific version of a package
await downloader.DownloadPackageAsync("Serilog", "2.5.0");
```
