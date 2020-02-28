# NuGet Package Downloader

NuGet package downloader is a package downloader library build in .Net to ease the download of NuGet packages from NuGet feeds or NuGet source repositories.

Most of the application these days are build as tools or plugins, the need of a tool to download and/or extract needful NuGet packages and its dependencies on demand has surfaced.

The library exposes below set of interfaces,

- **_Package Identities_**: Get package meta data information

- **_Download package_**: Download NuGet package from the source repositories

- **_Extract package_**: Extract package assemblies along with dependent package assemblies.

## Getting started

**_Package Identities_**<br/>

_GetPackageSearchMetadataAsync_,

Async method that exposes package identities through a collection of package metadata.

- Inputs<br/>

  _packageName_: Name of the NuGet package<br/>

  _targetFramework_: Enum field that defines the .Net target framework<br/>

  _downloadOptions_: Additional options that include<br/>

  - _Version_: Package version

  - _IncludePrerelease_: Include Pre release package

  - _NuGetSourceRepositories_: NuGet source respositories

  - _CancellationToken_: Token to abort the process<br/>

* Return<br/>

  _List of IPackageSearchMetadata_

<br/>**_Download package_**<br/>

_DownloadPackageAsync_, Async method to download NuGet package and its dependent package(s).

- Inputs<br/>

  _packageName_: Name of the NuGet package<br/>

  _targetFramework_: Enum field that defines the .Net target framework<br/>

  _outputPath_: Path to download the package(s)<br/>

  _downloadOptions_: Additional options that include<br/>

  - _Version_: Package version

  - _IncludePrerelease_: Include Pre release package

  - _NuGetSourceRepositories_: NuGet source respositories

  - _CancellationToken_: Token to abort the process

<br/>**_Extract package_**<br/>

_DownloadAndExtractPackageAsync_, Async method to download and extract package and dependent package assemblies.

- Inputs<br/>

  _packageName_: Name of the NuGet package<br/>

  _targetFramework_: Enum field that defines the .Net target framework<br/>

  _outputPath_: Path to download the package(s)<br/>

  _downloadOptions_: Additional options that include<br/>

  - _Version_: Package version

  - _IncludePrerelease_: Include Pre release package

  - _NuGetSourceRepositories_: NuGet source respositories

  - _CancellationToken_: Token to abort the process

## Detailed build status

| Branch | Appveyor                                                                                                                                                                             |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| dev    | [![Build status](https://ci.appveyor.com/api/projects/status/0f4u7344b392pfsv/branch/dev?svg=true)](https://ci.appveyor.com/project/Achi054/nugetpackagedownloader/branch/dev)       |
| master | [![Build status](https://ci.appveyor.com/api/projects/status/0f4u7344b392pfsv/branch/master?svg=true)](https://ci.appveyor.com/project/Achi054/nugetpackagedownloader/branch/master) |
