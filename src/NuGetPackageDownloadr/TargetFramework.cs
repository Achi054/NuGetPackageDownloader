using System;

using NuGet.Frameworks;

namespace NuGetPackageDownloader
{
    public enum TargetFramework
    {
        NetCoreApp3_1,
        NetCoreApp3_0,
        NetCoreApp2_2,
        NetCoreApp2_1,
        NetCoreApp2_0,
        NetStandard2_1,
        NetStandard2_0,
        Net48,
        Net472,
        Net471,
        Net47,
    }

    internal static class TargetFrameworkExtensions
    {
        internal static NuGetFramework ToNuGetFramework(this TargetFramework targetFramework)
        {
            string frameworkName = targetFramework switch
            {
                TargetFramework.NetStandard2_1 => ".NETStandard,Version=v2.1",
                TargetFramework.NetStandard2_0 => ".NETStandard,Version=v2.0",
                TargetFramework.NetCoreApp3_1 => ".NETCoreApp,Version=v3.1",
                TargetFramework.NetCoreApp3_0 => ".NETCoreApp,Version=v3.0",
                TargetFramework.NetCoreApp2_2 => ".NETCoreApp,Version=v2.2",
                TargetFramework.NetCoreApp2_1 => ".NETCoreApp,Version=v2.1",
                TargetFramework.NetCoreApp2_0 => ".NETCoreApp,Version=v2.0",
                TargetFramework.Net48 => ".NETFramework,Version=v4.8",
                TargetFramework.Net472 => ".NETFramework,Version=v4.7.2",
                TargetFramework.Net471 => ".NETFramework,Version=v4.7.1",
                TargetFramework.Net47 => ".NETFramework,Version=v4.7",
                _ => throw new ArgumentException(nameof(targetFramework))
            };

            return NuGetFramework.ParseFrameworkName(frameworkName, new DefaultFrameworkNameProvider());
        }
    }
}
